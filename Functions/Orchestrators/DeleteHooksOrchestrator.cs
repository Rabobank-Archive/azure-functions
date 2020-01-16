using Microsoft.Azure.WebJobs;
using Response = SecurePipelineScan.VstsService.Response;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Functions.Activities;
using Functions.Helpers;
using System.Threading;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Functions.Orchestrators
{
    public class DeleteHooksOrchestrator
    {
        private const int TimerInterval = 1;

        [FunctionName(nameof(DeleteHooksOrchestrator))]
        public async Task RunAsync([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var hooks = await context.CallActivityWithRetryAsync<IList<Response.Hook>>(nameof(GetHooksActivity),
                RetryHelper.ActivityRetryOptions, null);
            var accountName = context.GetInput<string>();

            await Task.WhenAll(hooks
                .Where(h => accountName == h.ConsumerInputs.AccountName)
                .Select(async (h, i) => await StartDeleteHooksActivityWithTimerAsync(context, h, i)));
        }

        private async static Task StartDeleteHooksActivityWithTimerAsync(
            IDurableOrchestrationContext context, Response.Hook hook, int index)
        {
            await context.CreateTimer(context.CurrentUtcDateTime.AddSeconds(index * TimerInterval), CancellationToken.None);
            await context.CallActivityWithRetryAsync(nameof(DeleteHooksActivity),
                RetryHelper.ActivityRetryOptions, hook);
        }
    }
}