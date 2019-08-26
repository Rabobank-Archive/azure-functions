using System.Collections.Generic;
using AzDoCompliancy.CustomStatus;
using Functions.Activities;
using Functions.Helpers;
using Functions.Model;
using Functions.Starters;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.VstsService.Response;
using Task = System.Threading.Tasks.Task;

namespace Functions.Orchestrators
{
    public class RepositoriesOrchestration
    {
        private readonly EnvironmentConfig _config;

        public RepositoriesOrchestration(EnvironmentConfig config)
        {
            _config = config;
        }
        
        [FunctionName(nameof(RepositoriesOrchestration))]
        public async Task Run([OrchestrationTrigger]DurableOrchestrationContextBase context)
        {
            var project = context.GetInput<Project>();
            context.SetCustomStatus(new ScanOrchestrationStatus
                {Project = project.Name, Scope = RuleScopes.Repositories});

            var repositories =
                await context.CallActivityWithRetryAsync<List<Repository>>(
                    nameof(RepositoriesForProjectActivity), RetryHelper.ActivityRetryOptions, project);

            //Not using select so we can run the activities each at a time
            var reports = new List<ItemExtensionData>();
            foreach (var repository in repositories)
            {
                var itemExtensionData = await context.CallActivityWithRetryAsync<ItemExtensionData>(nameof(RepositoriesScanActivity),
                    RetryHelper.ActivityRetryOptions, repository);
                reports.Add(itemExtensionData);
            }

            var data = new ItemsExtensionData
                {
                    Id = project.Name,
                    Date = context.CurrentUtcDateTime,
                    RescanUrl = ProjectScanHttpStarter.RescanUrl(_config, project.Name, RuleScopes.Repositories),
                    HasReconcilePermissionUrl = ReconcileFunction.HasReconcilePermissionUrl(_config, project.Id),
                    Reports = reports
                };
            

            await context.CallActivityAsync(nameof(LogAnalyticsUploadActivity),
                new LogAnalyticsUploadActivityRequest
                    {PreventiveLogItems = data.Flatten(RuleScopes.Repositories, context.InstanceId)});

            await context.CallActivityAsync(nameof(ExtensionDataUploadActivity),
                (repositories: data, RuleScopes.Repositories));
        }
    }
}