using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Functions.GlobalPermissionsScan;
using Functions.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SecurePipelineScan.VstsService.Requests;

namespace Functions.Starters
{
    public class GlobalPermissionsHttpStarter
    {
        private readonly EnvironmentConfig _config;
        private readonly ITokenizer _tokenizer;

        public GlobalPermissionsHttpStarter(
            EnvironmentConfig config,
            ITokenizer tokenizer)
        {
            _config = config;
            _tokenizer = tokenizer;
        }

        [FunctionName("GlobalPermissionsScanProject")]
        public async Task<IActionResult> RunFromHttp(
            [HttpTrigger(AuthorizationLevel.Anonymous, Route = "scan/{organization}/{project}/globalpermissions")]
            HttpRequestMessage request,
            string organization,
            string project,
            [OrchestrationClient] DurableOrchestrationClientBase orchestrationClientBase)
        {
            if (_tokenizer.IdentifierFromClaim(request) == null)
            {
                return new UnauthorizedResult();
            }
            await orchestrationClientBase.StartNewAsync(nameof(GlobalPermissionsScanProjectOrchestration), project);
            return new OkResult();
        }
    }
}
