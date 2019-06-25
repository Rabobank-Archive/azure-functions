using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Functions.GlobalPermissionsScan;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace Functions.Starters
{
    public class GlobalPermissionsHttpStarter
    {
        private readonly ITokenizer _tokenizer;

        public GlobalPermissionsHttpStarter(ITokenizer tokenizer)
        {
            _tokenizer = tokenizer;
        }

        [FunctionName(nameof(GlobalPermissionsHttpStarter))]
        public async Task<HttpResponseMessage> RunFromHttp(
            [HttpTrigger(AuthorizationLevel.Anonymous, Route = "scan/{organization}/{project}/globalpermissions")]
            HttpRequestMessage request,
            string organization,
            string project,
            [OrchestrationClient] DurableOrchestrationClientBase orchestrationClientBase)
        {
            if (_tokenizer.IdentifierFromClaim(request) == null)
            {
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }
            var instanceId = await orchestrationClientBase.StartNewAsync(nameof(GlobalPermissionsScanProjectOrchestration), project);
            return await orchestrationClientBase.WaitForCompletionOrCreateCheckStatusResponseAsync(request, instanceId,
                TimeSpan.FromSeconds(60));
        }
    }
}
