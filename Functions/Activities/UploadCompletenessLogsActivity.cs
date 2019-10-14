using System;
using System.Threading.Tasks;
using Functions.Model;
using LogAnalytics.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Functions.Activities
{
    public class UploadCompletenessLogsActivity
    {
        private readonly ILogAnalyticsClient _client;

        public UploadCompletenessLogsActivity(ILogAnalyticsClient client) => _client = client;

        [FunctionName(nameof(UploadCompletenessLogsActivity))]
        public async Task RunAsync([ActivityTrigger] CompletenessLogItem request, ILogger logger)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            await _client.AddCustomLogJsonAsync("completeness_log", new[] { request }, "AnalysisCompleted")
                .ConfigureAwait(false);

            logger.LogInformation(
                $"Analyzed completeness! Supervisor id: '{request.SupervisorId}', started at '{request.SupervisorStarted}'. " +
                $"Scanned projects {request.ScannedProjectCount}/{request.TotalProjectCount}");
        }
    }
}