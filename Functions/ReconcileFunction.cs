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
using Requests = SecurePipelineScan.VstsService.Requests;

namespace Functions
{
    public class ReconcileFunction
    {
        private readonly IRulesProvider _ruleProvider;
        private readonly ITokenizer _tokenizer;
        private readonly IVstsRestClient _client;

        private const int PermissionBit = 3;

        public ReconcileFunction(IVstsRestClient client, IRulesProvider ruleProvider, ITokenizer tokenizer)
        {
            _client = client;
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
                    return await ReconcileItemAsync(project, ruleName, item, _ruleProvider.RepositoryRules(_client));
                case RuleScopes.BuildPipelines:
                    return await ReconcileItemAsync(project, ruleName, item, _ruleProvider.BuildRules(_client));
                case RuleScopes.ReleasePipelines:
                    return await ReconcileItemAsync(project, ruleName, item, _ruleProvider.ReleaseRules(_client));
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

            return rule != null ? new Reconcile
            {
                Url = new Uri($"https://{environmentConfig.FunctionAppHostname}/api/reconcile/{environmentConfig.Organization}/{projectId}/{scope}/{rule.GetType().Name}/{itemId}"),
                Impact = rule.Impact
            } : null;
        }

        public static Reconcile ReconcileFromRule(EnvironmentConfig environmentConfig, string project, IProjectReconcile rule)
        {
            if (environmentConfig == null)
                throw new ArgumentNullException(nameof(environmentConfig));

            return rule != null ? new Reconcile
            {
                Url = new Uri($"https://{environmentConfig.FunctionAppHostname}/api/reconcile/{environmentConfig.Organization}/{project}/globalpermissions/{rule.GetType().Name}"),
                Impact = rule.Impact
            } : null;
        }

        public static Uri HasReconcilePermissionUrl(EnvironmentConfig environmentConfig, string projectId)
        {
            if (environmentConfig == null)
                throw new ArgumentNullException(nameof(environmentConfig));

            return new Uri($"https://{environmentConfig.FunctionAppHostname}/api/reconcile/{environmentConfig.Organization}/{projectId}/haspermissions");
        }

        private async Task<IActionResult> ReconcileGlobalPermissionsAsync(string project, string ruleName)
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

        private static async Task<IActionResult> ReconcileItemAsync(string project, string ruleName, string item, IEnumerable<IRule> rules)
        {
            var rule = rules
                .OfType<IReconcile>()
                .SingleOrDefault(x => x.GetType().Name == ruleName);

            if (rule == null)
            {
                return new NotFoundObjectResult($"Rule not found {ruleName}");
            }

            await rule.ReconcileAsync(project, item, null);
            return new OkResult();
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
            var permissions = await _client.GetAsync(Requests.Permissions.PermissionsGroupProjectId(project, id));

            if (permissions == null)
            {
                permissions = await _client.GetAsync(Requests.Permissions.PermissionsGroupProjectId(project, userId));

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