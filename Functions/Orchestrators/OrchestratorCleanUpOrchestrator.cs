using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Functions.Activities;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Functions.Orchestrators
{
    public class OrchestratorCleanUpOrchestrator
    {
        [FunctionName(nameof(OrchestratorCleanUpOrchestrator))]
        public async Task RunAsync([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var (runningOrchestratorIds, subOrchestratorIds) =
                await context.CallActivityAsync<(IList<string>, IList<string>)>(
                    nameof(GetOrchestratorsToPurgeActivity), null);

            await Task.WhenAll(runningOrchestratorIds.Select(f =>
              context.CallActivityAsync(nameof(TerminateOrchestratorActivity), f)));

            await context.CallActivityAsync(nameof(PurgeMultipleOrchestratorsActivity), null);

            await Task.WhenAll(subOrchestratorIds.Select(f =>
                context.CallActivityAsync(nameof(PurgeSingleOrchestratorActivity), f)));
        }
    }
}