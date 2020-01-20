using System.Collections.Generic;
using LogAnalytics.Client;
using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;
using Functions.Model;
using System;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Functions.Activities
{
    public class UploadConfigurationItemLogsActivity
    {
        private readonly ILogAnalyticsClient _client;

        public UploadConfigurationItemLogsActivity(ILogAnalyticsClient client) => _client = client;

        [FunctionName(nameof(UploadConfigurationItemLogsActivity))]
        public async Task RunAsync([ActivityTrigger] IEnumerable<ConfigurationItem> request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            foreach (var configurationItem in request)
            {
                await _client
                    .AddCustomLogJsonAsync("configuration_item_log", configurationItem, "evaluatedDate")
                    .ConfigureAwait(false);
            }
        }
    }
}