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
        public async Task RunAsync([OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            var projects = await context.CallActivityWithRetryAsync<IList<Response.Project>>(
                nameof(GetProjectsActivity),
                RetryHelper.ActivityRetryOptions, 
                null);
            var hooks = await context.CallActivityWithRetryAsync<IList<Response.Hook>>(
                nameof(GetHooksActivity),
                RetryHelper.ActivityRetryOptions, 
                null);

            await context.CallActivityAsync(nameof(CreateStorageQueuesActivity), null);
            await Task.WhenAll(projects.Select(async p => 
                await context.CallActivityWithRetryAsync(
                    nameof(CreateServiceHookSubscriptionsActivity),
                    RetryHelper.ActivityRetryOptions,
                    new CreateServiceHookSubscriptionsActivityRequest {Project = p, ExistingHooks = hooks})));
        }
    }
}