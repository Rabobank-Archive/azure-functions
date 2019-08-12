using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Functions.Completeness.Responses;
using Microsoft.Azure.WebJobs;

namespace Functions.Completeness.Activities
{
    public class GetAllOrchestratorsActivity
    {
        [FunctionName(nameof(GetAllOrchestratorsActivity))]
        public async Task<(IList<SimpleDurableOrchestrationStatus>, IList<SimpleDurableOrchestrationStatus>)> RunAsync(
            [ActivityTrigger] DurableActivityContextBase context, [OrchestrationClient] DurableOrchestrationClientBase client)
        {
            var runtimeStatuses = new List<OrchestrationRuntimeStatus>
            {
                OrchestrationRuntimeStatus.Completed,
                OrchestrationRuntimeStatus.Failed,
                OrchestrationRuntimeStatus.Canceled,
                OrchestrationRuntimeStatus.Terminated
            };

            var allOrchestrators = new List<DurableOrchestrationStatus>();
            var continuationToken = string.Empty;
            do
            {
                var orchestratorsPage = await client.GetStatusAsync(DateTime.Now.AddDays(-2), DateTime.Now, runtimeStatuses, 1000, continuationToken).ConfigureAwait(false);

                allOrchestrators.AddRange(orchestratorsPage.DurableOrchestrationState);

                continuationToken = orchestratorsPage.ContinuationToken;
            }
            while (Encoding.UTF8.GetString(Convert.FromBase64String(continuationToken)) != "null");

            return (
                allOrchestrators
                    .Where(x => x.Name == "ProjectScanSupervisor")
                    .Select(x => ConvertToSimpleDurableOrchestrationStatus(x))
                    .ToList(),
                allOrchestrators
                    .Where(x => x.Name == "ProjectScanOrchestration")
                    .Select(x => ConvertToSimpleDurableOrchestrationStatus(x))
                    .ToList()
            );
        }

        private SimpleDurableOrchestrationStatus ConvertToSimpleDurableOrchestrationStatus(DurableOrchestrationStatus orchestrator)
        {
            return new SimpleDurableOrchestrationStatus
            {
                Name = orchestrator.Name,
                InstanceId = orchestrator.InstanceId,
                CreatedTime = orchestrator.CreatedTime,
                RuntimeStatus = orchestrator.RuntimeStatus,
                CustomStatus = orchestrator.CustomStatus
            };
        }
    }
}