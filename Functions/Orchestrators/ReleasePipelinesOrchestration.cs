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
    public class ReleasePipelinesOrchestration
    {
        private readonly EnvironmentConfig _config;

        public ReleasePipelinesOrchestration(EnvironmentConfig config)
        {
            _config = config;
        }
        
        [FunctionName(nameof(ReleasePipelinesOrchestration))]
        public async Task RunAsync([OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            var project = context.GetInput<Project>();
            context.SetCustomStatus(new ScanOrchestrationStatus
                {Project = project.Name, Scope = RuleScopes.ReleasePipelines});

            var releaseDefinitions =
                await context.CallActivityWithRetryAsync<List<ReleaseDefinition>>(nameof(ReleaseDefinitionsForProjectActivity),
                    RetryHelper.ActivityRetryOptions,
                    project);
            
            //Not using select so we can run the activities each at a time
            var reports = new List<ItemExtensionData>();
            foreach (var releaseDefinition in releaseDefinitions)
            {
                var itemExtensionData = await context.CallActivityWithRetryAsync<ItemExtensionData>(nameof(ReleasePipelinesScanActivity),
                    RetryHelper.ActivityRetryOptions, new ReleasePipelinesScanActivityRequest
                    {
                        Project = project,
                        ReleaseDefinition = releaseDefinition,
                    });
                reports.Add(itemExtensionData);
            }

            var data = new ItemsExtensionData
            {
                Id = project.Name,
                Date = context.CurrentUtcDateTime,
                RescanUrl = ProjectScanHttpStarter.RescanUrl(_config, project.Name, RuleScopes.ReleasePipelines),
                HasReconcilePermissionUrl = ReconcileFunction.HasReconcilePermissionUrl(_config, project.Id),
                Reports = reports
            };

            await context.CallActivityAsync(nameof(LogAnalyticsUploadActivity),
                new LogAnalyticsUploadActivityRequest
                    {PreventiveLogItems = data.Flatten(RuleScopes.ReleasePipelines, context.InstanceId)});

            await context.CallActivityAsync(nameof(ExtensionDataUploadActivity),
                (releasePipelines: data, RuleScopes.ReleasePipelines));
        }
    }
}