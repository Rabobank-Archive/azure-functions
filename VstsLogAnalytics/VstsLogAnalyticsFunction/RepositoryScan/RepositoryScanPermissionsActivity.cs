using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;
using VstsLogAnalytics.Client;
using VstsLogAnalyticsFunction.Model;
using Project = SecurePipelineScan.VstsService.Response.Project;
using Task = System.Threading.Tasks.Task;

namespace VstsLogAnalyticsFunction.RepositoryScan
{
    public class RepositoryScanPermissionsActivity
    {
        private readonly ILogAnalyticsClient _client;
        private readonly IVstsRestClient _azuredo;
        private readonly IRulesProvider _rulesProvider;
        private readonly EnvironmentConfig _azuredoConfig;
        private readonly ITokenizer _tokenizer;


        public RepositoryScanPermissionsActivity(ILogAnalyticsClient client,
            IVstsRestClient azuredo,
            IRulesProvider rulesProvider,
            EnvironmentConfig azuredoConfig, 
            ITokenizer tokenizer)
        {
            _client = client;
            _azuredo = azuredo;
            _azuredoConfig = azuredoConfig;
            _rulesProvider = rulesProvider;
            _tokenizer = tokenizer;
        }

        [FunctionName(nameof(RepositoryScanPermissionsActivity))]
        public async Task RunAsActivity(
            [ActivityTrigger] DurableActivityContextBase context,
            ILogger log)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            var project = context.GetInput<Project>() ?? throw new Exception("No Project found in parameter DurableActivityContextBase");

            await Run(project.Name);
        }

        [FunctionName("RepositoryScan")]
        public async Task<IActionResult> RunFromHttp(
            [HttpTrigger(AuthorizationLevel.Anonymous, Route = "scan/{organization}/{project}/repository")]
            HttpRequestMessage request,
            string organization,
            string project,
            ILogger log)
        {
            if (_tokenizer.IdentifierFromClaim(request) == null)
            {
                return new UnauthorizedResult();
            }

            await Run(project);
            return new OkResult();
        }

        private async Task Run(string project)
        {
            var now = DateTime.UtcNow;
            var rules = _rulesProvider.RepositoryRules(_azuredo);
            var repositories = _azuredo.Get(Repository.Repositories(project));

            var data = new RepositoriesExtensionData
            {
                Id = project,
                Date = now,
                RescanUrl =  $"https://{_azuredoConfig.FunctionAppHostname}/api/scan/{_azuredoConfig.Organization}/{project}/repository",
                Reports = repositories.Select(repository => new RepositoryExtensionData
                {
                    Item = repository.Name,
                    Rules = rules.Select(rule => new EvaluatedRule
                    {
                        Name = rule.GetType().Name,
                        Status = rule.Evaluate(project, repository.Id),
                        Description = rule.Description
                    }).ToList()
                }).ToList()
            };

            foreach (var item in data.Flatten())
            {
                await _client.AddCustomLogJsonAsync("preventive_analysis_log", item, "evaluatedDate");
            }
            
            _azuredo.Put(ExtensionManagement.ExtensionData<RepositoriesExtensionData>("tas", _azuredoConfig.ExtensionName, "repository"), data);
        }
    }
}