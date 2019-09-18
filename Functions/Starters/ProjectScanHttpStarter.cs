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

namespace Functions.Starters
{
    public class ProjectScanHttpStarter
    {
        private readonly ITokenizer _tokenizer;
        private readonly IVstsRestClient _azuredo;
        private const int TimeOut = 180;

        private static readonly IDictionary<string, string> Scopes = new Dictionary<string, string>
        {
            [RuleScopes.GlobalPermissions] = nameof(GlobalPermissionsOrchestration),
            [RuleScopes.Repositories] = nameof(RepositoriesOrchestration),
            [RuleScopes.BuildPipelines] = nameof(BuildPipelinesOrchestration),
            [RuleScopes.ReleasePipelines] = nameof(ReleasePipelinesOrchestration)
        };

        public ProjectScanHttpStarter(ITokenizer tokenizer, IVstsRestClient azuredo)
        {
            _tokenizer = tokenizer;
            _azuredo = azuredo;
        }

        [FunctionName(nameof(ProjectScanHttpStarter))]
        public async Task<HttpResponseMessage> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, Route = "scan/{organization}/{project}/{scope}")]
            HttpRequestMessage request, string organization, string project, string scope,
            [OrchestrationClient] DurableOrchestrationClientBase starter)
        {
            if (_tokenizer.IdentifierFromClaim(request) == null)
            {
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            var orchestratorRequest = new ItemOrchestratorRequest
            {
                Project = await _azuredo.GetAsync(Project.ProjectByName(project)),
                ProductionItems = new List<ProductionItem>()
            };

            if (orchestratorRequest.Project == null)
                return new HttpResponseMessage(HttpStatusCode.NotFound);

            var instanceId = await starter.StartNewAsync(Orchestration(scope), orchestratorRequest);
            return await starter.WaitForCompletionOrCreateCheckStatusResponseAsync(request, instanceId, 
                TimeSpan.FromSeconds(TimeOut));
        }

        private static string Orchestration(string scope) =>
            Scopes.TryGetValue(scope, out var value) ? value : throw new ArgumentException(nameof(scope));

        public static string RescanUrl(EnvironmentConfig environmentConfig, string project, string scope) =>
            $"https://{environmentConfig.FunctionAppHostname}/api/scan/{environmentConfig.Organization}/" +
                $"{project}/{scope}";
    }
}