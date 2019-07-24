using Functions.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Requests = SecurePipelineScan.VstsService.Requests;

namespace Functions
{
    public class ReconcileFunction
    {
        private readonly IRulesProvider _ruleProvider;
        private readonly ITokenizer _tokenizer;
        private readonly IVstsRestClient _client;

        public ReconcileFunction(IVstsRestClient client, IRulesProvider ruleProvider, ITokenizer tokenizer)
        {
            _client = client;
            _ruleProvider = ruleProvider;
            _tokenizer = tokenizer;
        }

        [FunctionName(nameof(ReconcileFunction))]
        public async Task<IActionResult> Reconcile([HttpTrigger(AuthorizationLevel.Anonymous, Route = "reconcile/{organization}/{project}/{scope}/{ruleName}/{item?}")]HttpRequestMessage request,
            string organization,
            string project,
            string scope,
            string ruleName,
            string item = null)
        {
            var id = _tokenizer.IdentifierFromClaim(request);
            if (id == null || !(await HasPermissionToReconcile(project, id)))
            {
                return new UnauthorizedResult();
            }

            switch (scope)
            {
                case RuleScopes.GlobalPermissions:
                    return await ReconcileGlobalPermissions(project, ruleName);
                case RuleScopes.Repositories:
                    return await ReconcileItem(project, ruleName, item, _ruleProvider.RepositoryRules(_client));
                case RuleScopes.BuildPipelines:
                    return await ReconcileItem(project, ruleName, item, _ruleProvider.BuildRules(_client));
                case RuleScopes.ReleasePipelines:
                    return await ReconcileItem(project, ruleName, item, _ruleProvider.ReleaseRules(_client));
                default:
                    return new NotFoundObjectResult(scope);
            }
        }

        private async Task<IActionResult> ReconcileGlobalPermissions(string project, string ruleName)
        {
            var rule = _ruleProvider
                .GlobalPermissions(_client)
                .OfType<IProjectReconcile>()
                .SingleOrDefault(x => x.GetType().Name == ruleName);

            if (rule == null)
            {
                return new NotFoundObjectResult($"Rule not found {ruleName}");
            }

            await rule.ReconcileAsync(project);
            return new OkResult();
        }

        private static async Task<IActionResult> ReconcileItem(string project, string ruleName, string item, IEnumerable<IRule> rules)
        {
            var rule = rules
                .OfType<IReconcile>()
                .SingleOrDefault(x => x.GetType().Name == ruleName);

            if (rule == null)
            {
                return new NotFoundObjectResult($"Rule not found {ruleName}");
            }

            await rule.ReconcileAsync(project, item);
            return new OkResult();
        }

        [FunctionName("HasPermissionToReconcileFunction")]
        public async Task<IActionResult> HasPermission([HttpTrigger(AuthorizationLevel.Anonymous,
            Route = "reconcile/{organization}/{project}/haspermissions")]HttpRequestMessage request,
            string organization,
            string project)
        {
            var id = _tokenizer.IdentifierFromClaim(request);
            if (id == null)
            {
                return new UnauthorizedResult();
            }

            return new OkObjectResult(await HasPermissionToReconcile(project, id));
        }

        private async Task<bool> HasPermissionToReconcile(string project, string id)
        {
            var permissions = await _client.GetAsync(Requests.Permissions.PermissionsGroupProjectId(project, id));
            return permissions.Security.Permissions.Any(x =>
                x.DisplayName == "Manage project properties" && x.PermissionId == 3);
        }

        public static Reconcile ReconcileFromRule(IReconcile rule,
            EnvironmentConfig environmentConfig,
            string projectId,
            string scope,
            string itemId)
        {
            return rule != null ? new Reconcile
            {
                Url = $"https://{environmentConfig.FunctionAppHostname}/api/reconcile/{environmentConfig.Organization}/{projectId}/{scope}/{rule.GetType().Name}/{itemId}",
                Impact = rule.Impact
            } : null;
        }

        public static Reconcile ReconcileFromRule(EnvironmentConfig environmentConfig, string project, IProjectReconcile rule)
        {
            return rule != null ? new Reconcile
            {
                Url = $"https://{environmentConfig.FunctionAppHostname}/api/reconcile/{environmentConfig.Organization}/{project}/globalpermissions/{rule.GetType().Name}",
                Impact = rule.Impact
            } : null;
        }

        public static string HasReconcilePermissionUrl(EnvironmentConfig environmentConfig, string projectId)
        {
            return $"https://{environmentConfig.FunctionAppHostname}/api/reconcile/{environmentConfig.Organization}/{projectId}/haspermissions";
        }
    }
}