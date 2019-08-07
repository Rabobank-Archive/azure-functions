using System;
using System.Threading.Tasks;
using Functions.Completeness.Orchestrators;
using Microsoft.Azure.WebJobs;

namespace Functions.Completeness.Starters
{
    public class OrchestratorCleanUpStarter
    {
        [FunctionName("OrchestratorCleanUpStarter")]
        public Task RunAsync([TimerTrigger("0 0 5 * * *", RunOnStartup=false)]
            TimerInfo timerInfo, [OrchestrationClient] DurableOrchestrationClientBase orchestrationClientBase)
        {
            if (orchestrationClientBase == null)
                throw new ArgumentNullException(nameof(orchestrationClientBase));

            return RunInternalAsync(orchestrationClientBase);
        }

        private static async Task RunInternalAsync(DurableOrchestrationClientBase orchestrationClientBase)
        {
            await orchestrationClientBase.StartNewAsync(nameof(OrchestratorCleanUpOrchestrator), null)
                .ConfigureAwait(false);
        }
    }
}