using Microsoft.Azure.WebJobs;
using SecurePipelineScan.VstsService.Response;
using System.Collections.Generic;
using System.Linq;
using Task = System.Threading.Tasks.Task;
using Functions.Activities;

namespace Functions.Orchestrators
{
    public class DeleteServiceHooksSubscriptionsOrchestrator
    {
        [FunctionName(nameof(DeleteServiceHooksSubscriptionsOrchestrator))]
        public async Task Run([OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            var subscriptionsToDelete = context.GetInput<List<Hook>>();

            await Task.WhenAll(subscriptionsToDelete.Select(s =>
                context.CallActivityAsync(nameof(DeleteServiceHookSubscriptionActivity), s)));
        }
    }
}
