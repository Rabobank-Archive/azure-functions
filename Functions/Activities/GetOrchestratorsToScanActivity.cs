using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Functions.Model;
using Functions.Helpers;
using Functions.Orchestrators;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Functions.Activities
{
    public class GetOrchestratorsToScanActivity
    {
        [FunctionName(nameof(GetOrchestratorsToScanActivity))]
        public async Task<(IList<Orchestrator>, IList<Orchestrator>)> RunAsync(
            [ActivityTrigger] IDurableActivityContext context,
            [DurableClient] IDurableOrchestrationClient client)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            const int fromDaysAgo = 30;
            var runtimeStatuses = new List<OrchestrationRuntimeStatus>
            {
                OrchestrationRuntimeStatus.Canceled,
                OrchestrationRuntimeStatus.Completed,
                OrchestrationRuntimeStatus.ContinuedAsNew,
                OrchestrationRuntimeStatus.Failed,
                OrchestrationRuntimeStatus.Pending,
                OrchestrationRuntimeStatus.Running,
                OrchestrationRuntimeStatus.Terminated,
                OrchestrationRuntimeStatus.Unknown
            };

            var supervisors = new List<Orchestrator>();
            var projectScanners = new List<Orchestrator>();

            var condition = new OrchestrationStatusQueryCondition
            {
                CreatedTimeFrom = DateTime.Now.Date.AddDays(-fromDaysAgo),
                CreatedTimeTo = DateTime.Now,
                RuntimeStatus = runtimeStatuses,
                PageSize = 1000
            };

            string continuationToken;
            do
            {
                var orchestrationStatusQueryResult =
                    await client.GetStatusAsync(condition, default).ConfigureAwait(false);
                supervisors.AddRange(orchestrationStatusQueryResult.DurableOrchestrationState
                    .Where(x => x.Name == nameof(ProjectScanSupervisor))
                    .Select(OrchestrationHelper.ConvertToOrchestrator));
                projectScanners.AddRange(orchestrationStatusQueryResult.DurableOrchestrationState
                    .Where(x => x.Name == nameof(ProjectScanOrchestrator))
                    .Select(OrchestrationHelper.ConvertToOrchestrator));
                continuationToken = orchestrationStatusQueryResult.ContinuationToken;

            } while (Encoding.UTF8.GetString(Convert.FromBase64String(continuationToken)) != "null");

            return (supervisors, projectScanners);
        }
    }
}