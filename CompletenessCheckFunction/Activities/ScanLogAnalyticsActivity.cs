using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;

namespace CompletenessCheckFunction.Activities
{
    public class ScanLogAnalyticsActivity
    {
        [FunctionName(nameof(ScanLogAnalyticsActivity))]
        public async Task Run([OrchestrationTrigger] DurableOrchestrationContextBase context)
        {

        }
    }
}


