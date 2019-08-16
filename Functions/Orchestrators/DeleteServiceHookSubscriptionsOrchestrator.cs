using Microsoft.Azure.WebJobs;
using SecurePipelineScan.VstsService.Response;
using System.Collections.Generic;
using System.Linq;
using Task = System.Threading.Tasks.Task;
using Functions.Activities;
using Functions.Helpers;

namespace Functions.Orchestrators
{
    public class DeleteServiceHookSubscriptionsOrchestrator
    {
        [FunctionName(nameof(DeleteServiceHookSubscriptionsOrchestrator))]
        public async Task Run([OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            var subscriptionsToDelete = context.GetInput<List<Hook>>();

            await Task.WhenAll(subscriptionsToDelete.Select(s =>
                context.CallActivityWithRetryAsync(nameof(DeleteServiceHookSubscriptionActivity),
                    RetryHelper.ActivityRetryOptions, s)));
        }
    }
}
