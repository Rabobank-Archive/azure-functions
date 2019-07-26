using Microsoft.Azure.WebJobs;
using CompletenessCheckFunction.Requests;
using LogAnalytics.Client;

namespace CompletenessCheckFunction.Activities
{
    public class UploadAnalysisResultToLogAnalyticsActivity
    {
        private readonly ILogAnalyticsClient _client;

        public UploadAnalysisResultToLogAnalyticsActivity(ILogAnalyticsClient client)
        {
            _client = client;
        }
        
        [FunctionName(nameof(UploadAnalysisResultToLogAnalyticsActivity))]
        public void Run([ActivityTrigger] UploadAnalysisResultToLogAnalyticsActivityRequest request)
        {
            
        }
    }
}


