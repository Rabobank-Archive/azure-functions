using System.Threading.Tasks;
using Functions.Activities;
using Functions.Model;
using Microsoft.Azure.WebJobs;
using Response = SecurePipelineScan.VstsService.Response;

namespace Functions.Orchestrators
{
    public static class ReleasePipelinesOrchestration
    {
        [FunctionName(nameof(ReleasePipelinesOrchestration))]
        public static async Task Run([OrchestrationTrigger]DurableOrchestrationContextBase context)
        {
            var project = context.GetInput<Response.Project>();
            context.SetCustomStatus(new ScanOrchestratorStatus {Project = project.Name, Scope = "releasepipelines"});
            
            var data = await context.CallActivityAsync<ItemsExtensionData>(
                nameof(ReleasePipelinesScanActivity), project);
            
            await context.CallActivityAsync(nameof(LogAnalyticsUploadActivity),
                new LogAnalyticsUploadActivityRequest { PreventiveLogItems = data.Flatten(project.Name) });

            await context.CallActivityAsync(nameof(ExtensionDataUploadActivity), (releasePipelines: data, "releasepipelines"));
        }
    }
}