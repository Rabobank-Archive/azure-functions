using Functions.Orchestrators;
using Microsoft.Azure.WebJobs;
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
            await orchestrationClientBase.StartNewAsync(nameof(CreateServiceHookSubscriptionsOrchestrator), null);
        }
    }
}