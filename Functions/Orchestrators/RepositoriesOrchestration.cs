using AzDoCompliancy.CustomStatus;
using Functions.Activities;
using Functions.Model;
using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;
using Functions.Helpers;
using Response = SecurePipelineScan.VstsService.Response;

namespace Functions.Orchestrators
{
    public static class RepositoriesOrchestration
    {
        [FunctionName(nameof(RepositoriesOrchestration))]
        public static async Task Run([OrchestrationTrigger]DurableOrchestrationContextBase context)
        {
            var project = context.GetInput<Response.Project>();
            context.SetCustomStatus(new ScanOrchestrationStatus
                {Project = project.Name, Scope = RuleScopes.Repositories});

            var data = await context.CallActivityWithRetryAsync<ItemsExtensionData>(
                nameof(RepositoriesScanActivity), RetryHelper.ActivityRetryOptions, project);

            await context.CallActivityAsync(nameof(LogAnalyticsUploadActivity),
                new LogAnalyticsUploadActivityRequest
                    {PreventiveLogItems = data.Flatten(RuleScopes.Repositories, context.InstanceId)});

            await context.CallActivityAsync(nameof(ExtensionDataUploadActivity),
                (repositories: data, RuleScopes.Repositories));
        }
    }
}