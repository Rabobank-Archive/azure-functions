using System;
using System.Threading.Tasks;
using Functions.Completeness.Orchestrators;
using Microsoft.Azure.WebJobs;

namespace Functions.Completeness.Starters
{
    public class CompletenessCheckStarter
    {
        [FunctionName("CompletenessCheckStarter")]
        public Task RunAsync(
            [TimerTrigger("0 0 3 * * *", RunOnStartup=false)] TimerInfo timerInfo, 
            [OrchestrationClient] DurableOrchestrationClientBase orchestrationClientBase)
        {
            if (orchestrationClientBase == null)
                throw new ArgumentNullException(nameof(orchestrationClientBase));

            return RunInternalAsync(orchestrationClientBase);
        }

        private static async Task RunInternalAsync(DurableOrchestrationClientBase orchestrationClientBase)
        {
            await orchestrationClientBase.StartNewAsync(nameof(CompletenessCheckOrchestrator), null)
                .ConfigureAwait(false);
        }
    }
}