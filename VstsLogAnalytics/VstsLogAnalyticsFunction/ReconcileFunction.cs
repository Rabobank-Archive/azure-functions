using System.Linq;
using System.Net.Http;
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
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, Route = "reconcile/{organization}/{project}/globalpermissions/{ruleName}")]HttpRequestMessage request,
            string organization, 
            string project, 
            string ruleName)
        {
            if (request.Headers.Authorization == null)
            {
                return new UnauthorizedResult();
            }
            
            var principal = _tokenizer.Principal(request.Headers.Authorization.Parameter);
            var claim = principal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
            if (claim == null)
            {
                return new UnauthorizedResult();
            }
            
            var permissions = _client.Get(Requests.Permissions.PermissionsGroupProjectId(project, claim.Value));
            if (!permissions.Security.Permissions.Any(x => x.DisplayName == "Manage project properties" && x.PermissionId == 3))
            {
                return new UnauthorizedResult();
            }
            
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
    }
}