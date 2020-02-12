using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Functions.Activities
{
    public class GetOrchestratorsToPurgeActivity
    {
        [FunctionName(nameof(GetOrchestratorsToPurgeActivity))]
        public async Task<(IList<string>, IList<string>)> RunAsync(
            [ActivityTrigger] IDurableActivityContext context,
            [DurableClient] IDurableOrchestrationClient client)
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
                OrchestrationRuntimeStatus.Pending
            };

            var condition = new OrchestrationStatusQueryCondition
            {
                CreatedTimeFrom = DateTime.Now.Date.AddDays(-fromDaysAgo),
                CreatedTimeTo = DateTime.Now.Date.AddDays(-toDaysAgo),
                RuntimeStatus = runtimeStatuses,
                PageSize = 1000
            };

            var runningOrchestratorIds = new List<string>();
            var subOrchestratorIds = new List<string>();

            string continuationToken;
            do
            {
                var orchestrationStatusQueryResult =
                    await client.GetStatusAsync(condition, default).ConfigureAwait(false);
                runningOrchestratorIds.AddRange(orchestrationStatusQueryResult.DurableOrchestrationState
                    .Where(x => x.RuntimeStatus == OrchestrationRuntimeStatus.Running)
                    .Select(x => x.InstanceId));
                subOrchestratorIds.AddRange(orchestrationStatusQueryResult.DurableOrchestrationState
                    .Where(x => x.Name != "ProjectScanSupervisor" && x.Name != "ProjectScanOrchestrator")
                    .Select(x => x.InstanceId));
                continuationToken = orchestrationStatusQueryResult.ContinuationToken;

            } while (Encoding.UTF8.GetString(Convert.FromBase64String(continuationToken)) != "null");
        
            return (runningOrchestratorIds, subOrchestratorIds);
        }
    }
}