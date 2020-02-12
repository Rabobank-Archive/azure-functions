using Microsoft.AspNetCore.Mvc;
using SecurePipelineScan.VstsService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Functions.Cmdb.Client;
using Functions.Cmdb.Model;
using Newtonsoft.Json.Serialization;
using SecurePipelineScan.VstsService.Requests;
using SecurePipelineScan.VstsService.Response;
using SecurePipelineScan.Rules.Security;

namespace Functions
{

    public class ReleasePipelineHasDeploymentMethodReconciler : IReleasePipelineHasDeploymentMethodReconciler
    {
        private readonly ICmdbClient _cmdbClient;
        private readonly IProductionItemsResolver _productionItemsResolver;
        private readonly IVstsRestClient _vstsClient;

        private const string AzureDevOpsDeploymentMethod = "Azure Devops";
        private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            }
        };

        public ReleasePipelineHasDeploymentMethodReconciler(IVstsRestClient vstsClient, ICmdbClient cmdbClient, IProductionItemsResolver productionItemsResolver)
        {
            _vstsClient = vstsClient;
            _cmdbClient = cmdbClient;
            _productionItemsResolver = productionItemsResolver;
        }

        public async Task<IActionResult> ReconcileAsync(string projectId, string itemId, string userId, string ciIdentifier, string environment)
        {
            if (projectId is null)
                throw new ArgumentNullException(nameof(projectId));
            if (itemId is null)
                throw new ArgumentNullException(nameof(itemId));
            if (userId is null)
                throw new ArgumentNullException(nameof(userId));

            var user = await GetUserAsync(userId).ConfigureAwait(false);
            var ci = await GetCiAsync(ciIdentifier ?? _cmdbClient.Config.NonProdCiIdentifier).ConfigureAwait(false);
            var assignmentGroup = await GetAssignmentGroupAsync(ci?.Device?.AssignmentGroup).ConfigureAwait(false);
            var isProdConfigurationItem = IsProdConfigurationItem(ciIdentifier ?? _cmdbClient.Config.NonProdCiIdentifier);

            if (isProdConfigurationItem && !IsUserEntitledForCi(user, assignmentGroup))
                return new UnauthorizedResult();

            var stages = await _productionItemsResolver.ResolveAsync(projectId, itemId);

            foreach (var stage in stages)
                await UpdateDeploymentMethodAsync(projectId, itemId, stage, ci).ConfigureAwait(false);

            if (isProdConfigurationItem)
                await RemoveDeploymentMethodFromNonProdConfigurationItemAsync(projectId, itemId).ConfigureAwait(false);

            return new OkResult();
        }

        private async System.Threading.Tasks.Task RemoveDeploymentMethodFromNonProdConfigurationItemAsync(string projectId, string itemId)
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

        private async System.Threading.Tasks.Task UpdateDeploymentMethodAsync(string projectId, string itemId, string productionStage, CiContentItem ci)
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