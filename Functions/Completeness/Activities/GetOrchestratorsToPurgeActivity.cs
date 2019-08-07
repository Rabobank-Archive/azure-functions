using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;

namespace Functions.Completeness.Activities
{
    public class GetOrchestratorsToPurgeActivity
    {
        private const int DaysOld = 14;

        [FunctionName(nameof(GetOrchestratorsToPurgeActivity))]
        public async Task<IList<DurableOrchestrationStatus>> Run([ActivityTrigger] DurableActivityContextBase context,
            [OrchestrationClient] DurableOrchestrationClientBase client)
        {
            var orchestrators = (await client.GetStatusAsync())
                .Where(i => i.RuntimeStatus == OrchestrationRuntimeStatus.Completed && i.CreatedTime < DateTime.Now.Date.AddDays(-DaysOld))
                .ToList();

            return orchestrators;
        }
    }
}