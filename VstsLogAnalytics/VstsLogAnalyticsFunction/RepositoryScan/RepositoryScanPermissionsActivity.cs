using System;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
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
        }

        [FunctionName(nameof(RepositoryScanPermissionsActivity))]
        public async Task Run(
            [ActivityTrigger] DurableActivityContextBase context,
            ILogger log)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var project = context.GetInput<Project>();

            if (project == null) throw new Exception("No Project found in parameter DurableActivityContextBase");

            try
            {
                var repositories = _azuredo.Get(Repository.Repositories(project.Name));
                foreach (var repository in repositories)
                {
                    await Run(project.Name, repository.Name, log);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private async Task Run(string project, string repository, ILogger log)
        {
            log.LogInformation($"Creating preventive analysis log for repository {repository} in project {project}");
            var dateTimeUtcNow = DateTime.UtcNow;

            var globalRepositoryPermissions = _rulesProvider.RepositoryRules(_azuredo);

            var evaluatedRules = globalRepositoryPermissions.Select(r => new
            {
                scope = "repository",
                rule = r.GetType().Name,
                description = r.Description,
                status = r.Evaluate(project, repository),
                project,
                evaluatedDate = dateTimeUtcNow
            }).ToList();

            log.LogInformation(
                $"Writing preventive analysis log for repository {repository} in project {project} to Log Analytics Workspace");
            foreach (var rule in evaluatedRules)
            {
                try
                {
                    await _client.AddCustomLogJsonAsync("preventive_analysis_log", new
                    {
                        rule.scope,
                        rule.rule,
                        rule.status,
                        rule.project,
                        rule.evaluatedDate
                    }, "evaluatedDate");
                }
                catch (Exception ex)
                {
                    log.LogError(ex, $"Failed to write report to log analytics: {ex}");
                    throw;
                }
            }
            
            try
            {
                var extensionData = new RepositoryExtensionData()
                {
                    Id = project,
                    Date = dateTimeUtcNow,
                    Reports = evaluatedRules.Select(r => new EvaluatedRule
                    {
                        Description = r.description,
                        Status = r.status
                    }).ToList()
                };
                _azuredo.Put(ExtensionManagement.ExtensionData<RepositoryExtensionData>("tas",
                    _azuredoConfig.ExtensionName,
                    "repository"), extensionData);
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Write Extension data failed: {ex}");
                throw;
            }
        }
    }
}