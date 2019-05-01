using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using Requests = SecurePipelineScan.VstsService.Requests;

namespace VstsLogAnalyticsFunction
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
        public IActionResult Reconcile([HttpTrigger(AuthorizationLevel.Anonymous, Route = "reconcile/{organization}/{project}/{scope}/{ruleName}/{item?}")]HttpRequestMessage request,
            string organization, 
            string project, 
            string scope,
            string ruleName,
            string item = null)
        {
            var id = _tokenizer.IdentifierFromClaim(request);
            if (id == null || !HasPermissionToReconcile(project, id))
            {
                return new UnauthorizedResult();
            }

            switch (scope)
            {
                case "globalpermissions":
                    return ReconcileGlobalPermissions(project, ruleName);
                case "repository":
                    return ReconcileRepository(project, ruleName, item);
                default:
                    return new NotFoundObjectResult(scope);
            }
        }

        private IActionResult ReconcileGlobalPermissions(string project, string ruleName)
        {
            var rule = _ruleProvider
                .GlobalPermissions(_client)
                .OfType<IProjectReconcile>()
                .SingleOrDefault(x => x.GetType().Name == ruleName);

            if (rule == null)
            {
                return new NotFoundObjectResult($"Rule not found {ruleName}");
            }

            rule.Reconcile(project);
            return new OkResult();
        }

        private IActionResult ReconcileRepository(string project, string ruleName, string item)
        {
            var rule = _ruleProvider
                .RepositoryRules(_client)
                .OfType<IRepositoryReconcile>()
                .SingleOrDefault(x => x.GetType().Name == ruleName);

            if (rule == null)
            {
                return new NotFoundObjectResult($"Rule not found {ruleName}");
            }

            rule.Reconcile(project, item);
            return new OkResult();
        }

        [FunctionName("HasPermissionToReconcileFunction")]
        public async Task<IActionResult> HasPermission([HttpTrigger(AuthorizationLevel.Anonymous, Route = "reconcile/{organization}/{project}/haspermissions")]HttpRequestMessage request,
            string organization, 
            string project)
        {
            var id = _tokenizer.IdentifierFromClaim(request);
            if (id == null)
            {
                return new UnauthorizedResult();
            }
            
            return new OkObjectResult(HasPermissionToReconcile(project, id));
        }

        private bool HasPermissionToReconcile(string project, string id)
        {
            var permissions = _client.Get(Requests.Permissions.PermissionsGroupProjectId(project, id));
            return permissions.Security.Permissions.Any(x =>
                x.DisplayName == "Manage project properties" && x.PermissionId == 3);
        }
    }
}