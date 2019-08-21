using System.Collections.Generic;
using AzDoCompliancy.CustomStatus;
using Functions.Activities;
using Functions.Helpers;
using Functions.Model;
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
        public async Task Run([OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            var project = context.GetInput<Project>();
            context.SetCustomStatus(new ScanOrchestrationStatus
                {Project = project.Name, Scope = RuleScopes.ReleasePipelines});

            var releaseDefinitions =
                await context.CallActivityWithRetryAsync<List<ReleaseDefinition>>(nameof(ReleaseDefinitionsForProjectActivity),
                    RetryHelper.ActivityRetryOptions,
                    project);

            var data = await context.CallActivityWithRetryAsync<ItemsExtensionData>(
                nameof(ReleasePipelinesScanActivity), RetryHelper.ActivityRetryOptions, project);

            await context.CallActivityAsync(nameof(LogAnalyticsUploadActivity),
                new LogAnalyticsUploadActivityRequest
                    {PreventiveLogItems = data.Flatten(RuleScopes.ReleasePipelines, context.InstanceId)});

            await context.CallActivityAsync(nameof(ExtensionDataUploadActivity),
                (releasePipelines: data, RuleScopes.ReleasePipelines));
        }
    }
}