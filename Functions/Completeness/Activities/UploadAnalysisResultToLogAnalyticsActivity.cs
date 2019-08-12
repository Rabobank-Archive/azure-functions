using System;
using System.Threading.Tasks;
using Functions.Completeness.Model;
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
        public Task RunAsync([ActivityTrigger] CompletenessAnalysisResult request, ILogger logger)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return RunInternalAsync(request, logger);
        }

        private async Task RunInternalAsync(CompletenessAnalysisResult request, ILogger logger)
        {
            await _client.AddCustomLogJsonAsync("completeness_log", new[] { request }, "AnalysisCompleted").ConfigureAwait(false);

            logger.LogInformation(
                $"Analyzed completeness! Supervisor id: '{request.SupervisorOrchestratorId}', started at '{request.SupervisorStarted}'. " +
                $"Scanned projects {request.ScannedProjectCount}/{request.TotalProjectCount}");
        }
    }
}