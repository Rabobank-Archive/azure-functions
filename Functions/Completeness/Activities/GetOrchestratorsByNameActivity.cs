using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;

namespace Functions.Completeness.Activities
{
    public class GetOrchestratorsByNameActivity
    {
        [FunctionName(nameof(GetOrchestratorsByNameActivity))]
        public async Task<IList<DurableOrchestrationStatus>> Run([ActivityTrigger] string name,
            [OrchestrationClient] DurableOrchestrationClientBase client)
        {
            var runtimeStatuses = new List<OrchestrationRuntimeStatus>
            {
                OrchestrationRuntimeStatus.Completed,
                OrchestrationRuntimeStatus.Failed,
                OrchestrationRuntimeStatus.Canceled,
                OrchestrationRuntimeStatus.Terminated
            };

            return (await client.GetStatusAsync().ConfigureAwait(false))
                .Where(i => i.Name == name && runtimeStatuses.Contains(i.RuntimeStatus))
                .ToList();
        }
    }
}