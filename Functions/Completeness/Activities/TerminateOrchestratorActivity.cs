using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;

namespace Functions.Completeness.Activities
{
    public class TerminateOrchestratorActivity
    {
        [FunctionName(nameof(TerminateOrchestratorActivity))]
        public async Task RunAsync([ActivityTrigger] string instanceId,
            [OrchestrationClient] DurableOrchestrationClientBase client)
        {
            await client.TerminateAsync(instanceId, "Orchestrator is already running multiple days")
                .ConfigureAwait(false);
        }
    }
}