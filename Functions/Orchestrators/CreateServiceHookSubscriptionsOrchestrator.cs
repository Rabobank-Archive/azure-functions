using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using Response = SecurePipelineScan.VstsService.Response;
using System.Threading.Tasks;
using Functions.Activities;
using Functions.Helpers;

namespace Functions.Orchestrators
{
    public class CreateServiceHookSubscriptionsOrchestrator

    {
        [FunctionName(nameof(CreateServiceHookSubscriptionsOrchestrator))]
        public async Task RunAsync([OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            var projects =
                await context.CallActivityWithRetryAsync<IList<Response.Project>>(nameof(GetProjectsActivity),
                    RetryHelper.ActivityRetryOptions, null);
            var existingHooks = await context.CallActivityWithRetryAsync<IList<Response.Hook>>(nameof(GetHooksActivity),
                RetryHelper.ActivityRetryOptions, null);

            await context.CallActivityAsync(nameof(CreateStorageQueuesActivity), null);

            foreach (var p in projects)
            {
                await context.CallActivityWithRetryAsync(nameof(CreateServiceHookSubscriptionsActivity),
                    RetryHelper.ActivityRetryOptions,
                    new CreateServiceHookSubscriptionsActivityRequest {Project = p, ExistingHooks = existingHooks});
            }
                
        }
    }
}