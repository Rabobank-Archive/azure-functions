using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SecurePipelineScan.Rules.Security;
using Functions.Cmdb.Client;
using Functions.Cmdb.Model;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;
using SecurePipelineScan.VstsService.Response;
using Task = System.Threading.Tasks.Task;

namespace Functions.Cmdb
{
    public class ReleasePipelineHasDeploymentMethod : IReleasePipelineRule, IReconcile
    {
        private const string AzureDevOpsDeploymentMethod = "Azure Devops";
        private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            }
        };
        private readonly IVstsRestClient _vstsClient;
        private readonly ICmdbClient _cmdbClient;

        public ReleasePipelineHasDeploymentMethod(IVstsRestClient vstsClient, ICmdbClient cmdbClient)
        {
            _vstsClient = vstsClient;
            _cmdbClient = cmdbClient;
        }

        [ExcludeFromCodeCoverage] public string Description => "Release pipeline has valid CMDB link";
        [ExcludeFromCodeCoverage] public string Link => "https://confluence.dev.somecompany.nl/x/PqKbD";
        [ExcludeFromCodeCoverage] public bool IsSox => false;
        [ExcludeFromCodeCoverage] public bool RequiresStageId => false;

        string[] IReconcile.Impact => new[] {
            "In the CMDB the deployment method for the CI is set to Azure DevOps and coupled to this release pipeline",
        };

        public Task<bool?> EvaluateAsync(string projectId, string stageId, ReleaseDefinition releasePipeline)
        {
            if (releasePipeline == null)
                throw new ArgumentNullException(nameof(releasePipeline));

            // TODO evaluate rule based on the configuration items in the next sprint (starting 6-feb-2020)
            return Task.FromResult<bool?>(stageId == "NON-PROD" || releasePipeline.Environments.Any(e => e.Id == stageId));
        }

        public async Task ReconcileAsync(string projectId, string itemId, string stageId, string userId, object data = null)
        {
            (string ciIdentifier, string productionStage) = GetData(data ?? new { });

            var user = await GetUserAsync(userId).ConfigureAwait(false);
            var ci = await GetCiAsync(ciIdentifier).ConfigureAwait(false);
            var assignmentGroup = await GetAssignmentGroupAsync(ci?.Device?.AssignmentGroup).ConfigureAwait(false);
            var isProdConfigurationItem = IsProdConfigurationItem(ciIdentifier);

            if (isProdConfigurationItem && !IsUserEntitledForCi(user, assignmentGroup))
                return;

            await UpdateDeploymentMethodAsync(projectId, itemId, productionStage, ci).ConfigureAwait(false);

            if (isProdConfigurationItem)
                await RemoveDeploymentMethodFromNonProdConfigurationItemAsync(projectId, itemId).ConfigureAwait(false);
        }

        private async Task RemoveDeploymentMethodFromNonProdConfigurationItemAsync(string projectId, string itemId)
        {
            var ci = await GetCiAsync(_cmdbClient.Config.NonProdCiIdentifier).ConfigureAwait(false);
            var deploymentMethods = ci?.Device?.DeploymentInfo ?? new DeploymentInfo[0];

            if (!deploymentMethods.Any())
                return;

            var index = deploymentMethods.ToList().FindIndex(x =>
            {
                if (x.DeploymentMethod != AzureDevOpsDeploymentMethod)
                    return false;

                var supplementaryInfo = ParseSupplementaryInfo(x.SupplementaryInformation);
                return supplementaryInfo.Project == projectId && supplementaryInfo.Pipeline == itemId;
            });

            if (index < 0)
                return;

            var update = deploymentMethods.Select((x, i) =>
                i == index ? new DeploymentInfo { SupplementaryInformation = null, DeploymentMethod = null } : x
            );

            await _cmdbClient.UpdateDeploymentMethodAsync(ci.Device.ConfigurationItem, CreateCiContentItemUpdate(ci, update))
                             .ConfigureAwait(false);
        }

        private static CiContentItem CreateCiContentItemUpdate(CiContentItem ci, IEnumerable<DeploymentInfo> update) =>
            new CiContentItem
            {
                Device = new ConfigurationItemModel
                {
                    DeploymentInfo = update,
                    AssignmentGroup = ci.Device.AssignmentGroup,
                    ConfigurationItem = ci.Device.ConfigurationItem
                }
            };

        private bool IsProdConfigurationItem(string ciIdentifier) => !_cmdbClient.Config.NonProdCiIdentifier.Equals(ciIdentifier);

        private static bool IsUserEntitledForCi(UserEntitlement user, AssignmentContentItem assignmentGroup) =>
            assignmentGroup?.Assignment?.Operators != null &&
            assignmentGroup.Assignment.Operators.Any(o => o.Equals(user?.User?.PrincipalName, StringComparison.OrdinalIgnoreCase));

        private async Task<AssignmentContentItem> GetAssignmentGroupAsync(string assignmentGroup) =>
            string.IsNullOrEmpty(assignmentGroup) ?
                null :
                await _cmdbClient.GetAssignmentAsync(assignmentGroup).ConfigureAwait(false);

        private async Task<CiContentItem> GetCiAsync(string ciIdentifier) =>
            string.IsNullOrEmpty(ciIdentifier) ? null : await _cmdbClient.GetCiAsync(ciIdentifier).ConfigureAwait(false);

        private async Task<UserEntitlement> GetUserAsync(string userId) =>
            string.IsNullOrEmpty(userId) ?
                null :
                await _vstsClient.GetAsync(MemberEntitlementManagement.GetUserEntitlement(userId))
                                 .ConfigureAwait(false);

        private (string, string) GetData(object data)
        {
            dynamic dynamicData = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(data));
            var ciIdentifier = (string)dynamicData.ciIdentifier ?? _cmdbClient.Config.NonProdCiIdentifier;

            return (ciIdentifier, (string)dynamicData.environment);
        }

        private async Task UpdateDeploymentMethodAsync(string projectId, string itemId, string productionStage, CiContentItem ci)
        {
            var deploymentMethods = ci.Device?.DeploymentInfo ?? new DeploymentInfo[0];
            if (deploymentMethods.Where(x => x.DeploymentMethod == AzureDevOpsDeploymentMethod)
                                 .Select(x => ParseSupplementaryInfo(x.SupplementaryInformation))
                                 .Any(x => x.Project == projectId &&
                                           x.Pipeline == itemId &&
                                           x.Stage == productionStage))
                return;

            var newDeploymentMethod = CreateDeploymentMethod(projectId, itemId, productionStage);
            var update = deploymentMethods.Concat(new[] { newDeploymentMethod });

            await _cmdbClient.UpdateDeploymentMethodAsync(ci.Device.ConfigurationItem, CreateCiContentItemUpdate(ci, update))
                             .ConfigureAwait(false);
        }

        private DeploymentInfo CreateDeploymentMethod(string projectId, string itemId, string productionStage) => new DeploymentInfo
        {
            DeploymentMethod = AzureDevOpsDeploymentMethod,
            SupplementaryInformation = CreateSupplementaryInformation(projectId, itemId, productionStage)
        };

        private string CreateSupplementaryInformation(string projectId, string itemId, string productionStage) => JsonConvert.SerializeObject(new SupplementaryInformation
        {
            Organization = _cmdbClient.Config.Organization,
            Pipeline = itemId,
            Project = projectId,
            Stage = productionStage
        }, _serializerSettings);

        private SupplementaryInformation ParseSupplementaryInfo(string json)
        {
            try
            {
                return (String.IsNullOrWhiteSpace(json)) ? null :
                     JsonConvert.DeserializeObject<SupplementaryInformation>(json, _serializerSettings);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}

