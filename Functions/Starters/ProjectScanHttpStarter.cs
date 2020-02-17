using Functions.Orchestrators;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using SecurePipelineScan.VstsService.Security;
using SecurePipelineScan.Rules.Security;

namespace Functions.Starters
{
    public class ProjectScanHttpStarter
    {
        private readonly ITokenizer _tokenizer;
        private readonly IVstsRestClient _azuredo;
        private readonly IPoliciesResolver _policiesResolver;
        private const int TimeOut = 180;

        public ProjectScanHttpStarter(ITokenizer tokenizer, IVstsRestClient azuredo, IPoliciesResolver policiesResolver)
        {
            _tokenizer = tokenizer;
            _azuredo = azuredo;
            _policiesResolver = policiesResolver;
        }

        [FunctionName(nameof(ProjectScanHttpStarter))]
        public async Task<HttpResponseMessage> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, Route = "scan/{organization}/{projectName}/{scope}")]
            HttpRequestMessage request, string organization, string projectName, string scope,
            [DurableClient] IDurableOrchestrationClient starter)
        {
            if (starter == null)
                throw new ArgumentNullException(nameof(starter));

            if (_tokenizer.IdentifierFromClaim(request) == null)
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);

            var project = await _azuredo.GetAsync(Project.ProjectByName(projectName));
            if (project == null)
                return new HttpResponseMessage(HttpStatusCode.NotFound);

            // clear cache for manual scan
            _policiesResolver.Clear(project.Id);

            var scanDate = DateTime.UtcNow;
            var instanceId = await starter.StartNewAsync<object>(nameof(ProjectScanOrchestrator), (project, scope, scanDate));
            return await starter.WaitForCompletionOrCreateCheckStatusResponseAsync(request, instanceId,
                TimeSpan.FromSeconds(TimeOut));
        }

        public static Uri RescanUrl(EnvironmentConfig environmentConfig, string project, string scope)
        {
            if (environmentConfig == null)
                throw new ArgumentNullException(nameof(environmentConfig));

            return new Uri($"https://{environmentConfig.FunctionAppHostname}/api/scan/{environmentConfig.Organization}/" +
                $"{project}/{scope}");
        }
    }
}