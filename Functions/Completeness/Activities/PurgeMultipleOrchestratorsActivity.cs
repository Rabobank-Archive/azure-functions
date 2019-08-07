using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DurableTask.Core;
using Microsoft.Azure.WebJobs;

namespace Functions.Completeness.Activities
{
    public class PurgeMultipleOrchestratorsActivity
    {
        [FunctionName(nameof(PurgeMultipleOrchestratorsActivity))]
        public async Task RunAsync([ActivityTrigger] DurableActivityContextBase context,
            [OrchestrationClient] DurableOrchestrationClientBase client)
        {
            const int PurgeFromDaysAgo = 365;
            const int KeepFromDaysAgo = 14;

            var runtimeStatuses = new List<OrchestrationStatus>
            {
                OrchestrationStatus.Completed,
                OrchestrationStatus.Failed,
                OrchestrationStatus.Canceled,
                OrchestrationStatus.Terminated
            };

            await client.PurgeInstanceHistoryAsync(DateTime.Now.Date.AddDays(-PurgeFromDaysAgo), 
                    DateTime.Now.Date.AddDays(-KeepFromDaysAgo), runtimeStatuses)
                .ConfigureAwait(false);
        }
    }
}