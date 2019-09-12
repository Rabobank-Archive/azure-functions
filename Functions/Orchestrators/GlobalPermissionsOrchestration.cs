using AzDoCompliancy.CustomStatus;
using Functions.Activities;
using Functions.Model;
using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;
using Functions.Helpers;

namespace Functions.Orchestrators
{
    public static class GlobalPermissionsOrchestration
    {
        [FunctionName(nameof(GlobalPermissionsOrchestration))]
        public static async Task RunAsync([OrchestrationTrigger]DurableOrchestrationContextBase context)
        {
            var request = context.GetInput<ItemOrchestratorRequest>();

            context.SetCustomStatus(new ScanOrchestrationStatus { Project = request.Project.Name, Scope = RuleScopes.GlobalPermissions });

            var data = await context.CallActivityWithRetryAsync<GlobalPermissionsExtensionData>
                (nameof(GlobalPermissionsScanProjectActivity), RetryHelper.ActivityRetryOptions, request.Project);

            await context.CallActivityAsync(
                nameof(ExtensionDataGlobalPermissionsUploadActivity), (permissions: data, RuleScopes.GlobalPermissions));

            await context.CallActivityAsync(nameof(LogAnalyticsUploadActivity),
                new LogAnalyticsUploadActivityRequest { PreventiveLogItems = data.Flatten(context.InstanceId) });
        }
    }
}