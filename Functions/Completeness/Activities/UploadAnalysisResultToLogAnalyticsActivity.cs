using System;
using System.Threading.Tasks;
using Functions.Completeness.Model;
using Functions.Completeness.Requests;
using LogAnalytics.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Functions.Completeness.Activities
{
    public class UploadAnalysisResultToLogAnalyticsActivity
    {
        private readonly ILogAnalyticsClient _client;

        public UploadAnalysisResultToLogAnalyticsActivity(ILogAnalyticsClient client)
        {
            _client = client;
        }

        [FunctionName(nameof(UploadAnalysisResultToLogAnalyticsActivity))]
        public Task RunAsync([ActivityTrigger] UploadAnalysisResultToLogAnalyticsActivityRequest request, ILogger logger)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return RunInternalAsync(request, logger);
        }

        private async Task RunInternalAsync(UploadAnalysisResultToLogAnalyticsActivityRequest request, ILogger logger)
        {
            var data = new CompletenessAnalysisResult
            {
                AnalysisCompleted = request.AnalysisCompleted,
                SupervisorStarted = request.SupervisorStarted,
                SupervisorOrchestratorId = request.SupervisorOrchestratorId,
                TotalProjectCount = request.TotalProjectCount,
                ScannedProjectCount = request.ScannedProjectCount,
            };
            await _client.AddCustomLogJsonAsync("completeness_log", new[] { data }, "AnalysisCompleted").ConfigureAwait(false);

            logger.LogInformation(
                $"Analyzed completeness! Supervisor id: '{request.SupervisorOrchestratorId}', started at '{request.SupervisorStarted}'. " +
                $"Scanned projects {request.ScannedProjectCount}/{request.TotalProjectCount}");
        }
    }
}