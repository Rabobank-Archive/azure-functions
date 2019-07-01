using System.Threading.Tasks;
using Functions.Activities;
using Functions.Model;
using Microsoft.Azure.WebJobs;
using Response = SecurePipelineScan.VstsService.Response;

namespace Functions.Orchestrators
{
    public static class RepositoriesOrchestration
    {
        [FunctionName(nameof(RepositoriesOrchestration))]
        public static async Task Run([OrchestrationTrigger]DurableOrchestrationContextBase context)
        {
            var project = context.GetInput<Response.Project>();
            context.SetCustomStatus(new ScanOrchestratorStatus {Project = project.Name, Scope = "repository" });
            
            var data = await context.CallActivityAsync<ItemsExtensionData>(
                nameof(RepositoriesScanActivity), project);
            
            await context.CallActivityAsync(nameof(LogAnalyticsUploadActivity),
                new LogAnalyticsUploadActivityRequest { PreventiveLogItems = data.Flatten(project.Name) });

            await context.CallActivityAsync(nameof(ExtensionDataUploadActivity), (repositories: data, "repository"));
        }
    }
}