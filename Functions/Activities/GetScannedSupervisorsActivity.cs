using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LogAnalytics.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;

namespace Functions.Activities
{
    public class GetScannedSupervisorsActivity
    {
        private readonly ILogAnalyticsClient _client;

        public GetScannedSupervisorsActivity(ILogAnalyticsClient client) => _client = client;

        [FunctionName(nameof(GetScannedSupervisorsActivity))]
        public async Task<List<string>> RunAsync([ActivityTrigger] IDurableActivityContext context, ILogger logger)
        {
            var queryResponse = await _client
                .QueryAsync("completeness_log_CL | where TimeGenerated > ago(365d) | project SupervisorOrchestratorId_g")
                .ConfigureAwait(false);

            var results = queryResponse == null
                ? new List<string>()
                : queryResponse.tables[0].rows
                    .Select(x => x[0].ToString().Replace("-", ""))
                    .ToList();

            logger.LogInformation($"Completed {nameof(GetScannedSupervisorsActivity)}, scannedSupervisors:{results.Count}");
            return results;
        }
    }
}