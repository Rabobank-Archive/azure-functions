using Functions.Orchestrators;
using Microsoft.Azure.WebJobs;
using System;
using System.Threading.Tasks;

namespace Functions.Starters
{
    public class CreateServiceHookSubscriptionsStarter
    {
        [FunctionName(nameof(CreateServiceHookSubscriptionsStarter))]
        public async Task RunAsync(
            [TimerTrigger("0 0 7,19 * * *", RunOnStartup = false)] TimerInfo timerInfo,
            [OrchestrationClient] DurableOrchestrationClientBase orchestrationClientBase)
        {
            if (orchestrationClientBase == null)
                throw new ArgumentNullException(nameof(orchestrationClientBase));

            await orchestrationClientBase.StartNewAsync(nameof(CreateServiceHookSubscriptionsOrchestrator), null);
        }
    }
}