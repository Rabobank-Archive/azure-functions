using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DurableTask.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Functions.Activities
{
    public class PurgeMultipleOrchestratorsActivity
    {
        [FunctionName(nameof(PurgeMultipleOrchestratorsActivity))]
        public async Task RunAsync([ActivityTrigger] IDurableActivityContext context,
            [DurableClient] IDurableOrchestrationClient client)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            const int purgeFromDaysAgo = 365;
            const int keepFromDaysAgo = 30;

            var runtimeStatuses = new List<OrchestrationStatus>
            {
                OrchestrationStatus.Completed,
                OrchestrationStatus.Failed,
                OrchestrationStatus.Canceled,
                OrchestrationStatus.Terminated,
                OrchestrationStatus.ContinuedAsNew,
                OrchestrationStatus.Pending
            };

            await client.PurgeInstanceHistoryAsync(DateTime.Now.Date.AddDays(-purgeFromDaysAgo),
                    DateTime.Now.Date.AddDays(-keepFromDaysAgo), runtimeStatuses)
                .ConfigureAwait(false);
        }
    }
}