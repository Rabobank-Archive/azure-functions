using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using SecurePipelineScan.VstsService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Requests = SecurePipelineScan.VstsService.Requests;
using Newtonsoft.Json;
using Functions.Cmdb.Client;
using Functions.Cmdb.Model;
using Newtonsoft.Json.Serialization;
using SecurePipelineScan.VstsService.Requests;
using SecurePipelineScan.VstsService.Response;
using SecurePipelineScan.VstsService.Security;
using SecurePipelineScan.Rules.Security;

namespace Functions
{
    public class ReconcileReleasePipelineHasDeploymentMethodFunction
    {
        private readonly ITokenizer _tokenizer;
        private readonly ICmdbClient _cmdbClient;
        private readonly IProductionItemsResolver _productionItemsResolver;
        private readonly IVstsRestClient _vstsClient;
        private const int PermissionBit = 3;

        private const string AzureDevOpsDeploymentMethod = "Azure Devops";
        private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            }
        };

        public ReconcileReleasePipelineHasDeploymentMethodFunction(IVstsRestClient vstsClient, ITokenizer tokenizer, ICmdbClient cmdbClient, IProductionItemsResolver productionItemsResolver)
        {
            _vstsClient = vstsClient;
            _tokenizer = tokenizer;
            _cmdbClient = cmdbClient;
            _productionItemsResolver = productionItemsResolver;
        }

        [FunctionName(nameof(ReconcileReleasePipelineHasDeploymentMethodFunction))]
        public async Task<IActionResult> ReconcileAsync([HttpTrigger(AuthorizationLevel.Anonymous, Route = "reconcile/{organization}/{project}/releasepipelines/ReleasePipelineHasDeploymentMethod/{item?}")]HttpRequestMessage request,
            string organization,
            string project,
            string item = null)
        {
            if (string.IsNullOrWhiteSpace(project))
                throw new ArgumentNullException(nameof(project));

            var id = _tokenizer.IdentifierFromClaim(request);

            if (id == null)
                return new UnauthorizedResult();

            var userId = GetUserIdFromQueryString(request);

            if (!(await HasPermissionToReconcileAsync(project, id, userId)))
                return new UnauthorizedResult();

            var (ciIdentifier, environment) = await GetData(request);

            return await ReconcileAsync(project, id, userId, ciIdentifier, environment);
        }

        private async Task<IActionResult> ReconcileAsync(string projectId, string itemId, string userId, string ciIdentifier, string environment)
        {
            var user = await GetUserAsync(userId).ConfigureAwait(false);
            var ci = await GetCiAsync(ciIdentifier).ConfigureAwait(false);
            var assignmentGroup = await GetAssignmentGroupAsync(ci?.Device?.AssignmentGroup).ConfigureAwait(false);
            var isProdConfigurationItem = IsProdConfigurationItem(ciIdentifier);

            if (isProdConfigurationItem && !IsUserEntitledForCi(user, assignmentGroup))
                return new UnauthorizedResult();

            var stages = await _productionItemsResolver.ResolveAsync(projectId, itemId);

            foreach (var stage in stages)
                await UpdateDeploymentMethodAsync(projectId, itemId, stage, ci).ConfigureAwait(false);

            if (isProdConfigurationItem)
                await RemoveDeploymentMethodFromNonProdConfigurationItemAsync(projectId, itemId).ConfigureAwait(false);

            return new OkResult();
        }

        private async Task<(string, string)> GetData(HttpRequestMessage request)
        {
            if (request.Content == null)
                return (_cmdbClient.Config.NonProdCiIdentifier, null);

            var content = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
            dynamic data = JsonConvert.DeserializeObject(content);

            var ciIdentifier = (string)data.ciIdentifier ?? _cmdbClient.Config.NonProdCiIdentifier;

            return (ciIdentifier, (string)data.environment);
        }

        private static string GetUserIdFromQueryString(HttpRequestMessage request)
        {
            var query = request.RequestUri?.Query != null
                ? HttpUtility.ParseQueryString(request.RequestUri?.Query)
                : null;

            return query?.Get("userId");
        }

        private async Task<bool> HasPermissionToReconcileAsync(string project, string id, string userId = null)
        {
            var permissions = await _vstsClient.GetAsync(Requests.Permissions.PermissionsGroupProjectId(project, id)).ConfigureAwait(false);
            if (permissions == null)
            {
                permissions = await _vstsClient.GetAsync(Requests.Permissions.PermissionsGroupProjectId(project, userId)).ConfigureAwait(false);
                if (permissions == null)
                {
                    return false;
                }
            }

            return permissions.Security.Permissions.Any(x =>
                x.DisplayName == "Manage project properties" && x.PermissionId == PermissionBit);
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

        private (string, string) GetData(object data)
        {
            dynamic dynamicData = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(data));
            var ciIdentifier = (string)dynamicData.ciIdentifier ?? _cmdbClient.Config.NonProdCiIdentifier;

            return (ciIdentifier, (string)dynamicData.environment);
        }

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