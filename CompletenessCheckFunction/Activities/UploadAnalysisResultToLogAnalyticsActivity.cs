using Microsoft.Azure.WebJobs;
using CompletenessCheckFunction.Requests;
using LogAnalytics.Client;
using Microsoft.Extensions.Logging;

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
        public void Run([ActivityTrigger] UploadAnalysisResultToLogAnalyticsActivityRequest request, ILogger _logger)
        {
            _logger.LogWarning($"Analyzed completeness! Supervisor id: '{request.SupervisorOrchestratorId}', started at '{request.SupervisorStarted}'. Scanned projects {request.ScannedProjectCount}/{request.TotalProjectCount}");
        }
    }
}


