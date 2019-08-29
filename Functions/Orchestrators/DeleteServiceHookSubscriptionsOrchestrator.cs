using Microsoft.Azure.WebJobs;
using Response = SecurePipelineScan.VstsService.Response;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Functions.Activities;
using Functions.Helpers;

namespace Functions.Orchestrators
{
    public class DeleteServiceHookSubscriptionsOrchestrator
    {
        private readonly EnvironmentConfig _config;

        public DeleteServiceHookSubscriptionsOrchestrator(EnvironmentConfig config)
        {
            _config = config;
        }
        
        [FunctionName(nameof(DeleteServiceHookSubscriptionsOrchestrator))]
        public async Task RunAsync([OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            var allHookSubscriptions =
                await context.CallActivityWithRetryAsync<IList<Response.Hook>>(nameof(GetHooksActivity), RetryHelper.ActivityRetryOptions, null);

            var subscriptionsToDelete = allHookSubscriptions
                .Where(h => _config.EventQueueStorageAccountName == h.ConsumerInputs.AccountName)
                .ToList();
            
            foreach (var hook in subscriptionsToDelete)
            {
                await context.CallActivityWithRetryAsync(nameof(DeleteServiceHookSubscriptionActivity),
                    RetryHelper.ActivityRetryOptions, hook);
            }
        }
    }
}
