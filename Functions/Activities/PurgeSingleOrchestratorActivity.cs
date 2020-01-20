using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Functions.Activities
{
    public class PurgeSingleOrchestratorActivity
    {
        [FunctionName(nameof(PurgeSingleOrchestratorActivity))]
        public async Task RunAsync([ActivityTrigger] string instanceId,
            [DurableClient] IDurableOrchestrationClient client)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            await client.PurgeInstanceHistoryAsync(instanceId)
                .ConfigureAwait(false);
        }
    }
}