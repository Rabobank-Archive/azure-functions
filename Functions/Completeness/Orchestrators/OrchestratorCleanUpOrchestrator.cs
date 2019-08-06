using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Functions.Completeness.Activities;
using Microsoft.Azure.WebJobs;

namespace Functions.Completeness.Orchestrators
{
    public class OrchestratorCleanUpOrchestrator
    {
        [FunctionName(nameof(OrchestratorCleanUpOrchestrator))]
        public async Task RunAsync([OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            var orchestratorsToPurge = await context.CallActivityAsync<List<DurableOrchestrationStatus>>(
                nameof(GetOrchestratorsToPurgeActivity), null);

            await Task.WhenAll(orchestratorsToPurge.Select(f =>
                context.CallActivityAsync(nameof(PurgeSingleOrchestratorActivity), f.InstanceId)));
        }
    }
}