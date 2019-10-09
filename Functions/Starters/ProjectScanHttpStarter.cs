using Functions.Orchestrators;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Functions.Model;
using SecurePipelineScan.Rules.Security;

namespace Functions.Starters
{
    public class ProjectScanHttpStarter
    {
        private readonly ITokenizer _tokenizer;
        private readonly IVstsRestClient _azuredo;
        private const int TimeOut = 180;

        private static readonly IDictionary<string, string> Scopes = new Dictionary<string, string>
        {
            [RuleScopes.GlobalPermissions] = nameof(GlobalPermissionsOrchestrator),
            [RuleScopes.Repositories] = nameof(RepositoriesOrchestrator),
            [RuleScopes.BuildPipelines] = nameof(BuildPipelinesOrchestrator),
            [RuleScopes.ReleasePipelines] = nameof(ReleasePipelinesOrchestrator)
        };

        public ProjectScanHttpStarter(ITokenizer tokenizer, IVstsRestClient azuredo)
        {
            _tokenizer = tokenizer;
            _azuredo = azuredo;
        }

        [FunctionName(nameof(ProjectScanHttpStarter))]
        public async Task<HttpResponseMessage> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, Route = "scan/{organization}/{project}/{scope}")]
            HttpRequestMessage request, string organization, string projectName, string scope,
            [OrchestrationClient] DurableOrchestrationClientBase starter)
        {
            if (starter == null)
                throw new ArgumentNullException(nameof(starter));

            if (_tokenizer.IdentifierFromClaim(request) == null)
            {
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            var project = await _azuredo.GetAsync(Project.ProjectByName(projectName));
            var productionItems = new List<ProductionItem>();

            if (project == null)
                return new HttpResponseMessage(HttpStatusCode.NotFound);

            var instanceId = await starter.StartNewAsync(Orchestration(scope), (project, productionItems));
            return await starter.WaitForCompletionOrCreateCheckStatusResponseAsync(request, instanceId, 
                TimeSpan.FromSeconds(TimeOut));
        }

        private static string Orchestration(string scope) =>
            Scopes.TryGetValue(scope, out var value) ? value : throw new ArgumentException(nameof(scope));

        public static Uri RescanUrl(EnvironmentConfig environmentConfig, string project, string scope)
        {
            if (environmentConfig == null)
                throw new ArgumentNullException(nameof(environmentConfig));

            return new Uri($"https://{environmentConfig.FunctionAppHostname}/api/scan/{environmentConfig.Organization}/" +
                $"{project}/{scope}");
        }
    }
}