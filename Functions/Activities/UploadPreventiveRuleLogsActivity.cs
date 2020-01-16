using Functions.Model;
using LogAnalytics.Client;
using Microsoft.Azure.WebJobs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Functions.Activities
{
    public class UploadPreventiveRuleLogsActivity
    {
        private readonly ILogAnalyticsClient _client;

        public UploadPreventiveRuleLogsActivity(ILogAnalyticsClient client) => _client = client;

        [FunctionName(nameof(UploadPreventiveRuleLogsActivity))]
        public async Task RunAsync([ActivityTrigger] IEnumerable<PreventiveRuleLogItem> items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            foreach (var item in items)
            {
                await _client.AddCustomLogJsonAsync("preventive_analysis_log", item, "evaluatedDate")
                    .ConfigureAwait(false);
            }
        }
    }
}