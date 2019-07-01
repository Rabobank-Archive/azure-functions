using System.Threading.Tasks;
using Functions.Activities;
using Functions.Model;
using Microsoft.Azure.WebJobs;

namespace Functions.Orchestrators
{
    public static class GlobalPermissionsOrchestration
    {
        [FunctionName(nameof(GlobalPermissionsOrchestration))]
        public static async Task Run([OrchestrationTrigger]DurableOrchestrationContextBase context)
        {
            var project = context.GetInput<string>();
            context.SetCustomStatus(new ScanOrchestratorStatus {Project = project, Scope = "globalpermissions" });
            
            var data = await context.CallActivityAsync<GlobalPermissionsExtensionData>
                (nameof(GlobalPermissionsScanProjectActivity), project);

            await context.CallActivityAsync(
                nameof(ExtensionDataGlobalPermissionsUploadActivity), (permissions: data, "globalpermissions"));

            await context.CallActivityAsync(nameof(LogAnalyticsUploadActivity),
                new LogAnalyticsUploadActivityRequest {PreventiveLogItems = data.Flatten()});
        }
    }
}