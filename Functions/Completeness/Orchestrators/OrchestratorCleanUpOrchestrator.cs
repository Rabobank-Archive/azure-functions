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
            await context.CallActivityAsync(nameof(PurgeMultipleOrchestratorsActivity), null);
        }
    }
}