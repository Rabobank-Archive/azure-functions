using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;
using LogAnalytics.Client;

namespace CompletenessCheckFunction.Activities
{
    public class GetCompletedScansFromLogAnalyticsActivity
    {
        private readonly ILogAnalyticsClient _client;

        public GetCompletedScansFromLogAnalyticsActivity(ILogAnalyticsClient client)
        {
            _client = client;
        }
        [FunctionName(nameof(GetCompletedScansFromLogAnalyticsActivity))]
        public async Task<List<string>> Run([ActivityTrigger] DurableOrchestrationContextBase context)
        {
            // For now return an empty list. This means we'll just analyze everything
            return new List<string>();
        }
    }
}


