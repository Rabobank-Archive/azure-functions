using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using CompletenessCheckFunction.Requests;
using CompletenessCheckFunction.Tests.Activities;
using LogAnalytics.Client;
using Microsoft.Extensions.Logging;
using System;

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
        public Task RunAsync([ActivityTrigger] UploadAnalysisResultToLogAnalyticsActivityRequest request, ILogger _logger)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return RunInternalAsync(request, _logger);
        }

        private async Task RunInternalAsync(UploadAnalysisResultToLogAnalyticsActivityRequest request, ILogger _logger)
        {
            var data = new CompletenessAnalysisResult
            {
                AnalysisCompleted = request.AnalysisCompleted,
                SupervisorStarted = request.SupervisorStarted,
                SupervisorOrchestratorId = request.SupervisorOrchestratorId,
                TotalProjectCount = request.TotalProjectCount,
                ScannedProjectCount = request.TotalProjectCount
            };
            await _client.AddCustomLogJsonAsync("completeness_log", new[] { data }, "AnalysisCompleted").ConfigureAwait(false);

            _logger.LogInformation(
                $"Analyzed completeness! Supervisor id: '{request.SupervisorOrchestratorId}', started at '{request.SupervisorStarted}'. " +
                $"Scanned projects {request.ScannedProjectCount}/{request.TotalProjectCount}");
        }
    }
}