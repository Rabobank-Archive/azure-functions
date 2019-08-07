using AzDoCompliancy.CustomStatus;
using Functions.Activities;
using Functions.Model;
using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;
using Functions.Helpers;
using Response = SecurePipelineScan.VstsService.Response;

namespace Functions.Orchestrators
{
    public static class BuildPipelinesOrchestration
    {
        [FunctionName(nameof(BuildPipelinesOrchestration))]
        public static async Task Run([OrchestrationTrigger]DurableOrchestrationContextBase context)
        {
            var project = context.GetInput<Response.Project>();
            context.SetCustomStatus(new ScanOrchestrationStatus
                {Project = project.Name, Scope = RuleScopes.BuildPipelines});

            var data = await context.CallActivityWithRetryAsync<ItemsExtensionData>(
                nameof(BuildPipelinesScanActivity), RetryHelper.ActivityRetryOptions, project);

            await context.CallActivityAsync(nameof(LogAnalyticsUploadActivity),
                new LogAnalyticsUploadActivityRequest
                    {PreventiveLogItems = data.Flatten(RuleScopes.BuildPipelines, context.InstanceId)});

            await context.CallActivityAsync(nameof(ExtensionDataUploadActivity),
                (buildPipelines: data, RuleScopes.BuildPipelines));
        }
    }
}