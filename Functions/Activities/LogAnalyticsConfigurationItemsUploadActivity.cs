using System.Collections.Generic;
using LogAnalytics.Client;
using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;
using Functions.Model;
using System;

namespace Functions.Activities
{
    public class LogAnalyticsConfigurationItemsUploadActivity
    {
        private readonly ILogAnalyticsClient _analyticsClient;

        public LogAnalyticsConfigurationItemsUploadActivity(ILogAnalyticsClient analyticsClient)
        {
            _analyticsClient = analyticsClient;
        }

        [FunctionName(nameof(LogAnalyticsConfigurationItemsUploadActivity))]
        public async Task RunAsync([ActivityTrigger] IEnumerable<ConfigurationItem> request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            foreach (var configurationItem in request)
            {
                await _analyticsClient
                    .AddCustomLogJsonAsync("configuration_item_log", configurationItem, "evaluatedDate")
                    .ConfigureAwait(false);
            }
        }
    }
}