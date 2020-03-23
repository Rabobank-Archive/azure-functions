using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureDevOps.Compliance.Rules;
using Functions.Activities;
using Functions.Helpers;
using Functions.Model;
using Functions.Starters;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using SecurePipelineScan.VstsService.Response;
using Task = System.Threading.Tasks.Task;

namespace Functions.Orchestrators
{
    public class RepositoriesOrchestrator
    {
        private readonly EnvironmentConfig _config;

        public RepositoriesOrchestrator(EnvironmentConfig config) => _config = config;

        [FunctionName(nameof(RepositoriesOrchestrator))]
        public async Task RunAsync([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var (project, scanDate) =
                context.GetInput<(Project, DateTime)>();

            var repositories = await context.CallActivityWithRetryAsync<IEnumerable<Repository>>(nameof(GetRepositoriesActivity),
                RetryHelper.ActivityRetryOptions, project);

            var data = new ItemsExtensionData
            {
                Id = project.Name,
                Date = context.CurrentUtcDateTime,
                RescanUrl = ProjectScanHttpStarter.RescanUrl(_config, project.Name,
                    RuleScopes.Repositories),
                HasReconcilePermissionUrl = ReconcileFunction.HasReconcilePermissionUrl(_config,
                    project.Id),
                Reports = await Task.WhenAll(repositories.Select(r =>
                    StartScanActivityAsync(context, r, project)))
            };

            await context.CallActivityAsync(nameof(UploadExtensionDataActivity),
                (repositories: data, RuleScopes.Repositories));
        }

        private static Task<ItemExtensionData> StartScanActivityAsync(IDurableOrchestrationContext context,
                Repository repository, Project project) =>
            context.CallActivityWithRetryAsync<ItemExtensionData>(nameof(ScanRepositoriesActivity),
                RetryHelper.ActivityRetryOptions, (project, repository));
    }
}