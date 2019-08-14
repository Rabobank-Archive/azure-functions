using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;

namespace Functions.Completeness.Activities
{
    public class PurgeSingleOrchestratorActivity
    {
        [FunctionName(nameof(PurgeSingleOrchestratorActivity))]
        public async Task RunAsync([ActivityTrigger] string instanceId,
            [OrchestrationClient] DurableOrchestrationClientBase client)
        {
            await client.PurgeInstanceHistoryAsync(instanceId)
                .ConfigureAwait(false);
        }
    }
}