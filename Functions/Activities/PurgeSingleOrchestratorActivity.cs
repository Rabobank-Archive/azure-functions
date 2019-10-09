using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;

namespace Functions.Activities
{
    public class PurgeSingleOrchestratorActivity
    {
        [FunctionName(nameof(PurgeSingleOrchestratorActivity))]
        public async Task RunAsync([ActivityTrigger] string instanceId,
            [OrchestrationClient] DurableOrchestrationClientBase client)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            await client.PurgeInstanceHistoryAsync(instanceId)
                .ConfigureAwait(false);
        }
    }
}