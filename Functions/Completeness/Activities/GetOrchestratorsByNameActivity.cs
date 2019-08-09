using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

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

            var foundOrchestrators = new List<DurableOrchestrationStatus>();
            var continuationToken = string.Empty;
            do
            {
                var orchestratorsPage = await client.GetStatusAsync(DateTime.Now.AddDays(-2), DateTime.Now, runtimeStatuses, 1000, continuationToken).ConfigureAwait(false);

                foundOrchestrators.AddRange(orchestratorsPage.DurableOrchestrationState.Where(i => i.Name == name));

                continuationToken = orchestratorsPage.ContinuationToken;
            } while (Encoding.UTF8.GetString(Convert.FromBase64String(continuationToken)) != "null");  // It's magic :)

            return foundOrchestrators;
        }
    }
}