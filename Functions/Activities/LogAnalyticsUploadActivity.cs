using LogAnalytics.Client;
using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;

namespace Functions.Activities
{
    public class LogAnalyticsUploadActivity
    {
        private readonly ILogAnalyticsClient _analytics;

        public LogAnalyticsUploadActivity(ILogAnalyticsClient analytics)
        {
            _analytics = analytics;
        }

        [FunctionName(nameof(LogAnalyticsUploadActivity))]
        public async Task RunAsync([ActivityTrigger] LogAnalyticsUploadActivityRequest request)
        {
            foreach (var item in request.PreventiveLogItems)
            {
                await _analytics.AddCustomLogJsonAsync("preventive_analysis_log", item, "evaluatedDate")
                    .ConfigureAwait(false);
            }
        }
    }
}