using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;

namespace CompletenessCheckFunction.Activities
{
    public class GetCompletedScansFromLogAnalyticsActivity
    {
        [FunctionName(nameof(GetCompletedScansFromLogAnalyticsActivity))]
        public async Task Run([ActivityTrigger] DurableOrchestrationContextBase context)
        {

        }
    }
}


