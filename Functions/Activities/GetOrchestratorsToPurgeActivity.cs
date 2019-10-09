using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;

namespace Functions.Activities
{
    public class GetOrchestratorsToPurgeActivity
    {
        [FunctionName(nameof(GetOrchestratorsToPurgeActivity))]
        public async Task<(IList<string>, IList<string>)> RunAsync(
            [ActivityTrigger] DurableActivityContextBase context,
            [OrchestrationClient] DurableOrchestrationClientBase client)
        {
            const int fromDaysAgo = 30;
            const int toDaysAgo = 2;
            var runtimeStatuses = new List<OrchestrationRuntimeStatus>
            {
                OrchestrationRuntimeStatus.Completed,
                OrchestrationRuntimeStatus.Failed,
                OrchestrationRuntimeStatus.Canceled,
                OrchestrationRuntimeStatus.Terminated,
                OrchestrationRuntimeStatus.Running,
                OrchestrationRuntimeStatus.ContinuedAsNew,
                OrchestrationRuntimeStatus.Pending,
                OrchestrationRuntimeStatus.Unknown
            };

            var runningOrchestratorIds = new List<string>();
            var subOrchestratorIds = new List<string>();
            var continuationToken = string.Empty;

            do
            {
                var orchestratorsPage = await client.GetStatusAsync(DateTime.Now.Date.AddDays(-fromDaysAgo), 
                        DateTime.Now.Date.AddDays(-toDaysAgo), runtimeStatuses, 1000, continuationToken)
                    .ConfigureAwait(false);
                runningOrchestratorIds.AddRange(orchestratorsPage.DurableOrchestrationState
                    .Where(x => x.RuntimeStatus == OrchestrationRuntimeStatus.Running)
                    .Select(x => x.InstanceId));
                subOrchestratorIds.AddRange(orchestratorsPage.DurableOrchestrationState
                    .Where(x => x.Name != "ProjectScanSupervisor" && x.Name != "ProjectScanOrchestrator")
                    .Select(x => x.InstanceId));
                continuationToken = orchestratorsPage.ContinuationToken;
            }
            while (Encoding.UTF8.GetString(Convert.FromBase64String(continuationToken)) != "null");
        
            return (runningOrchestratorIds, subOrchestratorIds);
        }
    }
}