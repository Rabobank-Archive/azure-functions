using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Functions.Orchestrators;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace Functions.Starters
{
    public class ProjectScanHttpStarter
    {
        private readonly ITokenizer _tokenizer;

        private static readonly IDictionary<string, string> Scopes = new Dictionary<string, string>
        {
            ["globalpermissions"] = nameof(GlobalPermissionsOrchestration),
            ["buildpipelines"] = nameof(BuildPipelinesOrchestration),
            ["releasepipelines"] = nameof(ReleasePipelinesOrchestration)
        };

        public ProjectScanHttpStarter(ITokenizer tokenizer)
        {
            _tokenizer = tokenizer;
        }

        [FunctionName(nameof(ProjectScanHttpStarter))]
        public async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, Route = "scan/{organization}/{project}/{scope}")]
            HttpRequestMessage request,
            string organization,
            string project,
            string scope,
            [OrchestrationClient] DurableOrchestrationClientBase starter)
        {
            if (_tokenizer.IdentifierFromClaim(request) == null)
            {
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            var instanceId = await starter.StartNewAsync(Orchestration(scope), project);
            return await starter.WaitForCompletionOrCreateCheckStatusResponseAsync(request, instanceId, TimeSpan.FromSeconds(180));
        }

        private static string Orchestration(string scope) => 
            Scopes.TryGetValue(scope, out var value) ? value : throw new ArgumentException(nameof(scope));

        public static string RescanUrl(EnvironmentConfig environmentConfig, string project, string scope) => 
            $"https://{environmentConfig.FunctionAppHostname}/api/scan/{environmentConfig.Organization}/{project}/{scope}";
    }
}
