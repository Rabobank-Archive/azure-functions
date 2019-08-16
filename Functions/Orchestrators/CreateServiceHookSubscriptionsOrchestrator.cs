using System.Collections.Generic;
using System.Linq;
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
        public async Task Run([OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            var projects =
                await context.CallActivityWithRetryAsync<IList<Response.Project>>(nameof(GetProjectsActivity),
                    RetryHelper.ActivityRetryOptions, null);
            var existingHooks = await context.CallActivityWithRetryAsync<IList<Response.Hook>>(nameof(GetHooksActivity),
                RetryHelper.ActivityRetryOptions, null);

            await context.CallActivityAsync(nameof(CreateStorageQueuesActivity), null);

            await Task.WhenAll(projects.Select(p =>
                context.CallActivityWithRetryAsync(nameof(CreateServiceHookSubscriptionsActivity),
                    RetryHelper.ActivityRetryOptions,
                    new CreateServiceHookSubscriptionsActivityRequest {Project = p, ExistingHooks = existingHooks})));
        }
    }
}