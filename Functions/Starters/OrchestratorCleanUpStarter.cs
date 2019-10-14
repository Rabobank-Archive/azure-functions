using System;
using System.Threading.Tasks;
using Functions.Orchestrators;
using Microsoft.Azure.WebJobs;

namespace Functions.Starters
{
    public class OrchestratorCleanUpStarter
    {
        [FunctionName("OrchestratorCleanUpStarter")]
        public async Task RunAsync(
            [TimerTrigger("0 0 2 * * *", RunOnStartup=false)] TimerInfo timerInfo, 
            [OrchestrationClient] DurableOrchestrationClientBase orchestrationClientBase)
        {
            if (orchestrationClientBase == null)
                throw new ArgumentNullException(nameof(orchestrationClientBase));

            await orchestrationClientBase.StartNewAsync(nameof(OrchestratorCleanUpOrchestrator), null)
                .ConfigureAwait(false);
        }
    }
}