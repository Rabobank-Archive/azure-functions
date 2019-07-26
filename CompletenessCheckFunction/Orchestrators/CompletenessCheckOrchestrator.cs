using CompletenessCheckFunction.Activities;
using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;

namespace CompletenessCheckFunction.Orchestrators
{
    public class CompletenessCheckOrchestrator
    {
        [FunctionName(nameof(CompletenessCheckOrchestrator))]
        public async Task Run([OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            var data = await context.CallActivityAsync<LogAnalyticsSupervisorData>
                (nameof(ScanLogAnalyticsActivity), null);
        }
    }
}


