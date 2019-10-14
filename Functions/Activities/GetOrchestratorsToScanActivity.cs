using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Functions.Model;
using Functions.Helpers;
using Functions.Orchestrators;
using Microsoft.Azure.WebJobs;

namespace Functions.Activities
{
    public class GetOrchestratorsToScanActivity
    {
        [FunctionName(nameof(GetOrchestratorsToScanActivity))]
        public async Task<(IList<Orchestrator>, IList<Orchestrator>)> RunAsync(
            [ActivityTrigger] DurableActivityContextBase context,
            [OrchestrationClient] DurableOrchestrationClientBase client)
        {
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
            var continuationToken = string.Empty;

            do
            {
                var orchestratorsPage = await client.GetStatusAsync(DateTime.Now.Date.AddDays(-fromDaysAgo), 
                        DateTime.Now, runtimeStatuses, 1000, continuationToken)
                    .ConfigureAwait(false);
                supervisors.AddRange(orchestratorsPage.DurableOrchestrationState
                    .Where(x => x.Name == nameof(ProjectScanSupervisor))
                    .Select(OrchestrationHelper.ConvertToOrchestrator));
                projectScanners.AddRange(orchestratorsPage.DurableOrchestrationState
                    .Where(x => x.Name == nameof(ProjectScanOrchestrator))
                    .Select(OrchestrationHelper.ConvertToOrchestrator));
                continuationToken = orchestratorsPage.ContinuationToken;
            } while (Encoding.UTF8.GetString(Convert.FromBase64String(continuationToken)) != "null");

            return (supervisors, projectScanners);
        }
    }
}