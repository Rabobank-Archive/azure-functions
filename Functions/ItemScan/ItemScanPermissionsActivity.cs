using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Functions.Model;
using LogAnalytics.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing.Internal;
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

            log.LogInformation($"Executing {ActivityNameRepos} for project {project.Name}");

            try
            {
                await Run(project.Name, project.Id, "repository", log);
                log.LogInformation($"Executed {ActivityNameRepos} for project {project.Name}");
            }
            catch (Exception e)
            {
                log.LogInformation($"Execution failed {ActivityNameRepos} for project {project.Name}");
            }
        }

        [FunctionName(ActivityNameBuilds)]
        public async Task RunAsActivityBuilds(
            [ActivityTrigger] DurableActivityContextBase context,
            ILogger log)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            var project = context.GetInput<Response.Project>() ?? throw new Exception("No Project found in parameter DurableActivityContextBase");

            log.LogInformation($"Executing {ActivityNameBuilds} for project {project.Name}");
            
            try
            {
                await Run(project.Name, project.Id, "buildpipelines", log);
                log.LogInformation($"Executed {ActivityNameBuilds} for project {project.Name}");
            }
            catch (Exception e)
            {
                log.LogInformation($"Execution failed {ActivityNameBuilds} for project {project.Name}");
            }
        }

        [FunctionName(ActivityNameReleases)]
        public async Task RunAsActivityReleases(
            [ActivityTrigger] DurableActivityContextBase context,
            ILogger log)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            var project = context.GetInput<Response.Project>() ?? throw new Exception("No Project found in parameter DurableActivityContextBase");

            log.LogInformation($"Executing {ActivityNameReleases} for project {project.Name}");
            
            try
            {
                await Run(project.Name, project.Id, "releasepipelines", log);
                log.LogInformation($"Executed {ActivityNameReleases} for project {project.Name}");
            }
            catch (Exception e)
            {
                log.LogInformation($"Execution failed {ActivityNameReleases} for project {project.Name}");
            }
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

            var properties = _azuredo.Get(Requests.Project.Properties(project));

            await Run(project, properties.Id, scope, log);
            return new OkResult();
        }

        private async Task Run(string projectName, string projectId, string scope, ILogger log)
        {
            
            var now = DateTime.UtcNow;
            var data = new ItemsExtensionData
            {
                Id = projectName,
                Date = now,
                RescanUrl = $"https://{_config.FunctionAppHostname}/api/scan/{_config.Organization}/{projectName}/{scope}",
                HasReconcilePermissionUrl = $"https://{_config.FunctionAppHostname}/api/reconcile/{_config.Organization}/{projectId}/haspermissions",
                Reports = CreateReports(projectId, scope)
            };

            foreach (var item in data.Flatten(scope))
            {
                await _client.AddCustomLogJsonAsync("preventive_analysis_log", item, "evaluatedDate");
            }
                        
            _azuredo.Put(Requests.ExtensionManagement.ExtensionData<ItemsExtensionData>("tas", _config.ExtensionName, scope), data);
            
            log.LogInformation($"Executed ItemScanPermissionActivity with scope {scope} for project {projectName}");
        }

        private IList<ItemExtensionData> CreateReports(string projectId, string scope)
        {
            switch (scope)
            {
                case "repository":
                    return CreateReportsForRepositories(projectId, scope);
                case "buildpipelines":
                    return CreateReportsForBuildPipelines(projectId, scope);
                case "releasepipelines":
                    return CreateReportsForReleasePipelines(projectId, scope);
                default:
                    throw new ArgumentException(nameof(scope));
            }
        }

        private IList<ItemExtensionData> CreateReportsForRepositories(string projectId, string scope)
        {
            var rules = _rulesProvider.RepositoryRules(_azuredo);
            var items = _azuredo.Get(Requests.Repository.Repositories(projectId));
            
            return items.Select(x => new ItemExtensionData
            {
                Item = x.Name,
                Rules = Evaluate(projectId, scope, x.Id, rules)
            }).ToList();
        }

        private IList<ItemExtensionData> CreateReportsForBuildPipelines(string projectId, string scope)
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
                    Rules = Evaluate(projectId, scope, pipeline.Id, rules)
                });
            }
            return evaluationResults;
        }
        
        private IList<ItemExtensionData> CreateReportsForReleasePipelines(string projectId, string scope)
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
                    Rules = Evaluate(projectId, scope, pipeline.Id, rules)
                });
            }
            return evaluationResults;
        }

        private IList<EvaluatedRule> Evaluate(string projectId, string scope, string itemId, IEnumerable<IRule> rules)
        {
            return rules.Select(rule => new EvaluatedRule
            {
                Name = rule.GetType().Name,
                Status = rule.Evaluate(projectId, itemId),
                Description = rule.Description,
                Why = rule.Why,
                Reconcile = ToReconcile(projectId, scope, itemId, rule as IReconcile)
            }).ToList();
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