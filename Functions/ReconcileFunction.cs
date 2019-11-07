using Functions.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Functions.Activities;
using Microsoft.WindowsAzure.Storage.Table;
using Requests = SecurePipelineScan.VstsService.Requests;

namespace Functions
{
    public class ReconcileFunction
    {
        private readonly IRulesProvider _ruleProvider;
        private readonly ITokenizer _tokenizer;
        private readonly IVstsRestClient _vstsClient;
        private readonly CloudTableClient _tableClient;
        private readonly EnvironmentConfig _config;
        private const int PermissionBit = 3;

        public ReconcileFunction(EnvironmentConfig config, CloudTableClient tableClient, IVstsRestClient client, IRulesProvider ruleProvider, ITokenizer tokenizer)
        {
            _config = config;
            _vstsClient = client;
            _tableClient = tableClient;
            _ruleProvider = ruleProvider;
            _tokenizer = tokenizer;
        }

        [FunctionName(nameof(ReconcileFunction))]
        public async Task<IActionResult> ReconcileAsync([HttpTrigger(AuthorizationLevel.Anonymous, Route = "reconcile/{organization}/{project}/{scope}/{ruleName}/{item?}")]HttpRequestMessage request,
            string organization,
            string project,
            string scope,
            string ruleName,
            string item = null)
        {
            if (string.IsNullOrWhiteSpace(project))
                throw new ArgumentNullException(nameof(project));
            if (string.IsNullOrWhiteSpace(scope))
                throw new ArgumentNullException(nameof(scope));
            if (string.IsNullOrWhiteSpace(ruleName))
                throw new ArgumentNullException(nameof(ruleName));

            var id = _tokenizer.IdentifierFromClaim(request);

            if (id == null)
                return new UnauthorizedResult();

            var userId = GetUserIdFromQueryString(request);

            if (!(await HasPermissionToReconcileAsync(project, id, userId)))
                return new UnauthorizedResult();

            switch (scope)
            {
                case RuleScopes.GlobalPermissions:
                    return await ReconcileGlobalPermissionsAsync(project, ruleName);
                case RuleScopes.Repositories:
                    return await ReconcileItemAsync(project, ruleName, item, scope, _ruleProvider.RepositoryRules(_vstsClient));
                case RuleScopes.BuildPipelines:
                    return await ReconcileItemAsync(project, ruleName, item, scope, _ruleProvider.BuildRules(_vstsClient));
                case RuleScopes.ReleasePipelines:
                    return await ReconcileItemAsync(project, ruleName, item, scope, _ruleProvider.ReleaseRules(_vstsClient));
                default:
                    return new NotFoundObjectResult(scope);
            }
        }

        [FunctionName("HasPermissionToReconcileFunction")]
        public async Task<IActionResult> HasPermissionAsync([HttpTrigger(AuthorizationLevel.Anonymous,
            Route = "reconcile/{organization}/{project}/haspermissions")]HttpRequestMessage request,
            string organization,
            string project)
        {
            if (string.IsNullOrWhiteSpace(project))
                throw new ArgumentNullException(nameof(project));
            var id = _tokenizer.IdentifierFromClaim(request);
            if (id == null)
                return new UnauthorizedResult();

            var userId = GetUserIdFromQueryString(request);

            return new OkObjectResult(await HasPermissionToReconcileAsync(project, id, userId));
        }

        public static Reconcile ReconcileFromRule(IReconcile rule,
            EnvironmentConfig environmentConfig,
            string projectId,
            string scope,
            string itemId)
        {
            if (environmentConfig == null)
                throw new ArgumentNullException(nameof(environmentConfig));
            if (string.IsNullOrWhiteSpace(projectId))
                throw new ArgumentNullException(nameof(projectId));
            if (string.IsNullOrWhiteSpace(scope))
                throw new ArgumentNullException(nameof(scope));
            if (string.IsNullOrWhiteSpace(itemId))
                throw new ArgumentNullException(nameof(itemId));

            return rule != null
                ? new Reconcile
                {
                    Url = new Uri(
                        $"https://{environmentConfig.FunctionAppHostname}/api/reconcile/{environmentConfig.Organization}/{projectId}/{scope}/{rule.GetType().Name}/{itemId}"),
                    Impact = rule.Impact
                }
                : null;
        }

        public static Reconcile ReconcileFromRule(EnvironmentConfig environmentConfig, string projectId, IProjectReconcile rule)
        {
            if (environmentConfig == null)
                throw new ArgumentNullException(nameof(environmentConfig));
            if (string.IsNullOrWhiteSpace(projectId))
                throw new ArgumentNullException(nameof(projectId));

            return rule != null ? new Reconcile
            {
                Url = new Uri($"https://{environmentConfig.FunctionAppHostname}/api/reconcile/{environmentConfig.Organization}/{projectId}/globalpermissions/{rule.GetType().Name}"),
                Impact = rule.Impact
            } : null;
        }

        public static Uri HasReconcilePermissionUrl(EnvironmentConfig environmentConfig, string projectId)
        {
            if (environmentConfig == null)
                throw new ArgumentNullException(nameof(environmentConfig));
            if (string.IsNullOrWhiteSpace(projectId))
                throw new ArgumentNullException(nameof(projectId));

            return new Uri($"https://{environmentConfig.FunctionAppHostname}/api/reconcile/{environmentConfig.Organization}/{projectId}/haspermissions");
        }

        private async Task<IActionResult> ReconcileGlobalPermissionsAsync(string project, string ruleName)
        {
            var rule = _ruleProvider
                .GlobalPermissions(_vstsClient)
                .OfType<IProjectReconcile>()
                .SingleOrDefault(x => x.GetType().Name == ruleName);

            if (rule == null)
            {
                return new NotFoundObjectResult($"Rule not found {ruleName}");
            }

            await rule.ReconcileAsync(project);
            return new OkResult();
        }

        private async Task<IActionResult> ReconcileItemAsync(string projectId, string ruleName, string item,string scope, IEnumerable<IRule> rules)
        {
            if (string.IsNullOrEmpty(item))
                throw new ArgumentNullException(nameof(item));

            var rule = rules
                .OfType<IReconcile>()
                .SingleOrDefault(x => x.GetType().Name == ruleName);

            if (rule == null)
            {
                return new NotFoundObjectResult($"Rule not found {ruleName}");
            }
            
            if (rule.RequiresStageId)
            {
                var productionStageIds = await GetProductionStageIdsAsync(projectId, item).ConfigureAwait(false);
                if (!productionStageIds.Any())
                {
                    throw new InvalidOperationException($"Rule '{rule.GetType().Name}' requires stageId's but none are provided");
                }

                await Task.WhenAll(productionStageIds.Select(async stageId =>
                        await rule.ReconcileAsync(projectId, item, scope, stageId).ConfigureAwait(false)))
                    .ConfigureAwait(false);
            }
            else
            {
                await rule.ReconcileAsync(projectId, item, scope, null).ConfigureAwait(false);
            }

            return new OkResult();
        }

        private async Task<List<string>> GetProductionStageIdsAsync(string projectId, string pipelineId)
        {
            var productionItems = await new GetDeploymentMethodsActivity(_tableClient, _config).RunAsync(projectId)
                .ConfigureAwait(false);

            return productionItems
                .Where(p => p.ItemId == pipelineId)
                .SelectMany(p => p.DeploymentInfo)
                .Where(d =>
                    !string.IsNullOrWhiteSpace(d.StageId)).Select(d => d.StageId).Distinct().ToList();
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
    }
}