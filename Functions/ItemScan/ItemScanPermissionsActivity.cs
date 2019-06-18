using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Functions.Model;
using LogAnalytics.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using Requests = SecurePipelineScan.VstsService.Requests;
using Response = SecurePipelineScan.VstsService.Response;
using Task = System.Threading.Tasks.Task;


namespace Functions.ItemScan
{
    public class ItemScanPermissionsActivity
    {
        public const string ActivityNameRepos = nameof(ItemScanPermissionsActivity) + nameof(RunAsActivityRepos);
        public const string ActivityNameBuilds = nameof(ItemScanPermissionsActivity) + nameof(RunAsActivityBuilds);
        public const string ActivityNameReleases = nameof(ItemScanPermissionsActivity) + nameof(RunAsActivityReleases);

        private readonly ILogAnalyticsClient _client;
        private readonly IVstsRestClient _azuredo;
        private readonly IRulesProvider _rulesProvider;
        private readonly EnvironmentConfig _config;
        private readonly ITokenizer _tokenizer;

        public ItemScanPermissionsActivity(ILogAnalyticsClient client,
            IVstsRestClient azuredo,
            IRulesProvider rulesProvider,
            EnvironmentConfig config, 
            ITokenizer tokenizer)
        {
            _client = client;
            _azuredo = azuredo;
            _config = config;
            _rulesProvider = rulesProvider;
            _tokenizer = tokenizer;
        }
        
        [FunctionName(ActivityNameRepos)]
        public async Task RunAsActivityRepos(
            [ActivityTrigger] DurableActivityContextBase context,
            ILogger log)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            var project = context.GetInput<Response.Project>() ?? throw new Exception("No Project found in parameter DurableActivityContextBase");

            await Run(project.Name, project.Id, "repository");
        }

        [FunctionName(ActivityNameBuilds)]
        public async Task RunAsActivityBuilds(
            [ActivityTrigger] DurableActivityContextBase context,
            ILogger log)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            var project = context.GetInput<Response.Project>() ?? throw new Exception("No Project found in parameter DurableActivityContextBase");

            await Run(project.Name, project.Id, "buildpipelines");
        }

        [FunctionName(ActivityNameReleases)]
        public async Task RunAsActivityReleases(
            [ActivityTrigger] DurableActivityContextBase context,
            ILogger log)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            var project = context.GetInput<Response.Project>() ?? throw new Exception("No Project found in parameter DurableActivityContextBase");

            await Run(project.Name, project.Id, "releasepipelines");
        }


        [FunctionName(nameof(ItemScanPermissionsActivity) + nameof(RunFromHttp))]
        public async Task<IActionResult> RunFromHttp(
            [HttpTrigger(AuthorizationLevel.Anonymous, Route = "scan/{organization}/{project}/{scope}")]
            HttpRequestMessage request,
            string organization,
            string project,
            string scope,
            ILogger log)
        {
            if (_tokenizer.IdentifierFromClaim(request) == null)
            {
                return new UnauthorizedResult();
            }

            var properties = await _azuredo.GetAsync(Requests.Project.Properties(project));

            await Run(project, properties.Id, scope);
            return new OkResult();
        }

        private async Task Run(string projectName, string projectId, string scope)
        {
            var now = DateTime.UtcNow;
            var data = new ItemsExtensionData
            {
                Id = projectName,
                Date = now,
                RescanUrl = $"https://{_config.FunctionAppHostname}/api/scan/{_config.Organization}/{projectName}/{scope}",
                HasReconcilePermissionUrl = $"https://{_config.FunctionAppHostname}/api/reconcile/{_config.Organization}/{projectId}/haspermissions",
                Reports = await CreateReports(projectId, scope)
            };

            foreach (var item in data.Flatten(scope))
            {
                await _client.AddCustomLogJsonAsync("preventive_analysis_log", item, "evaluatedDate");
            }

            await _azuredo.PutAsync(Requests.ExtensionManagement.ExtensionData<ItemsExtensionData>("tas", _config.ExtensionName, scope), data);
        }

        private async Task<IList<ItemExtensionData>> CreateReports(string projectId, string scope)
        {
            switch (scope)
            {
                case "repository":
                    return await CreateReportsForRepositories(projectId, scope);
                case "buildpipelines":
                    return await CreateReportsForBuildPipelines(projectId, scope);
                case "releasepipelines":
                    return await CreateReportsForReleasePipelines(projectId, scope);
                default:
                    throw new ArgumentException(nameof(scope));
            }
        }

        private async Task<IList<ItemExtensionData>> CreateReportsForRepositories(string projectId, string scope)
        {
            var rules = _rulesProvider.RepositoryRules(_azuredo);
            var items = _azuredo.Get(Requests.Repository.Repositories(projectId));
            
            return await Task.WhenAll(items.Select(async x => new ItemExtensionData
            {
                Item = x.Name,
                Rules = await Evaluate(projectId, scope, x.Id, rules)
            }).ToList());
        }

        private async Task<IList<ItemExtensionData>> CreateReportsForBuildPipelines(string projectId, string scope)
        {
            var rules = _rulesProvider.BuildRules(_azuredo).ToList();
            var items = _azuredo.Get(Requests.Builds.BuildDefinitions(projectId));

            var evaluationResults = new List<ItemExtensionData>();
            
            // Do this in a loop (instead of in a Select) to avoid parallelism which messes up our sockets
            foreach (var pipeline in items)
            {
                evaluationResults.Add(new ItemExtensionData
                {
                    Item = pipeline.Name,
                    Rules = await Evaluate(projectId, scope, pipeline.Id, rules)
                });
            }
            return evaluationResults;
        }
        
        private async Task<IList<ItemExtensionData>> CreateReportsForReleasePipelines(string projectId, string scope)
        {
            var rules = _rulesProvider.ReleaseRules(_azuredo).ToList();
            var items = _azuredo.Get(Requests.ReleaseManagement.Definitions(projectId));

            var evaluationResults = new List<ItemExtensionData>();
            
            // Do this in a loop (instead of in a Select) to avoid parallelism which messes up our sockets
            foreach (var pipeline in items)
            {
                evaluationResults.Add(new ItemExtensionData
                {
                    Item = pipeline.Name,
                    Rules = await Evaluate(projectId, scope, pipeline.Id, rules)
                });
            }
            return evaluationResults;
        }

        private async Task<IList<EvaluatedRule>> Evaluate(string projectId, string scope, string itemId, IEnumerable<IRule> rules)
        {
            return await Task.WhenAll(rules.Select(async rule => new EvaluatedRule
            {
                Name = rule.GetType().Name,
                Status = await rule.Evaluate(projectId, itemId),
                Description = rule.Description,
                Why = rule.Why,
                Reconcile = ToReconcile(projectId, scope, itemId, rule as IReconcile)
            }).ToList());
        }

        private Reconcile ToReconcile(string projectId, string scope, string itemId, IReconcile rule)
        {
            return rule != null ? new Reconcile
            {
                Url = $"https://{_config.FunctionAppHostname}/api/reconcile/{_config.Organization}/{projectId}/{scope}/{rule.GetType().Name}/{itemId}",
                Impact =  rule.Impact
            } : null;
        }
    }
}