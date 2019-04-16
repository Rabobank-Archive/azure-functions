using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
using VstsLogAnalyticsFunction.GlobalPermissionsScan;

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
            var principal = _tokenizer.Principal(request.Headers.Authorization.Parameter);
            if (principal ==  null || 
                !principal.HasClaim("project", project) ||
                !principal.HasClaim("organization", organization))
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