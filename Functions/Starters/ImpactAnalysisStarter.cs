using System;
using System.Threading.Tasks;
using Functions.Orchestrators;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Functions.Starters
{
    public class ImpactAnalysisStarter
    {
        [FunctionName(nameof(ImpactAnalysisStarter))]
        public async Task RunAsync(
            [TimerTrigger("0 0 10 * * *", RunOnStartup = false)] TimerInfo timerInfo,
            [DurableClient] IDurableOrchestrationClient orchestrationClientBase)
        {
            if (orchestrationClientBase == null)
                throw new ArgumentNullException(nameof(orchestrationClientBase));

            await orchestrationClientBase.StartNewAsync(nameof(ImpactAnalysisOrchestrator))
                .ConfigureAwait(false);
        }
    }
}