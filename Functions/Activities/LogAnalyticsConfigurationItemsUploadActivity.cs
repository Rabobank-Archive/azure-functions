using System.Collections.Generic;
using LogAnalytics.Client;
using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;
using Functions.Model;

namespace Functions.Activities
{
    public class LogAnalyticsConfigurationItemsUploadActivity
    {
        private readonly ILogAnalyticsClient _analytics;

        public LogAnalyticsConfigurationItemsUploadActivity(ILogAnalyticsClient analytics)
        {
            _analytics = analytics;
        }

        [FunctionName(nameof(LogAnalyticsConfigurationItemsUploadActivity))]
        public async Task RunAsync([ActivityTrigger] IEnumerable<ConfigurationItem> request)
        {
        }
    }
}
