using System.Threading.Tasks;
using CompletenessCheckFunction.Orchestrators;
using Microsoft.Azure.WebJobs;

namespace CompletenessCheckFunction.Starters
{
    public class CompletenessCheckStarter
    {
        [FunctionName("CompletenessCheckStarter")]
        public async Task Run([TimerTrigger("0 17 3 * * *", RunOnStartup=false)]
            TimerInfo timerInfo, [OrchestrationClient] DurableOrchestrationClientBase orchestrationClientBase)
        {
            await orchestrationClientBase.StartNewAsync(nameof(CompletenessCheckOrchestrator), null);
        }
    }
}
