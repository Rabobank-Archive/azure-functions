using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using LogAnalytics.Client;
using System.Threading.Tasks;
using System.Linq;

namespace CompletenessCheckFunction.Activities
{
    public class GetCompletedScansFromLogAnalyticsActivity
    {
        private readonly ILogAnalyticsClient _client;

        public GetCompletedScansFromLogAnalyticsActivity(ILogAnalyticsClient client)
        {
            _client = client;
        }

        [FunctionName(nameof(GetCompletedScansFromLogAnalyticsActivity))]
        public async Task<List<string>> RunAsync([ActivityTrigger] DurableActivityContextBase context)
        {
            var queryResponse = await _client
                .QueryAsync("completeness_log_CL | where TimeGenerated > ago(365d) | project SupervisorOrchestratorId_g")
                .ConfigureAwait(false);

            if (queryResponse == null)
                return new List<string>();

            return queryResponse.tables[0].rows
                .Select(x => x[0].ToString())
                .ToList();
        }
    }
}