using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SecurePipelineScan.Rules;
using SecurePipelineScan.VstsService;
using System;
using System.Threading.Tasks;
using VstsLogAnalytics.Client;
using VstsLogAnalytics.Common;

namespace VstsLogAnalyticsFunction
{
    public static class BranchPoliciesFunction
    {
        [FunctionName("BranchPoliciesFunction")]
        public static async Task Run([TimerTrigger("0 */30 * * * *")] TimerInfo timerInfo,
            [Inject]ILogAnalyticsClient logAnalyticsClient,
            [Inject] IVstsRestClient client,
            ILogger log)
        {
            try
            {
                log.LogInformation($"Branch Policies timed check start: {DateTime.Now}");

                var scan = new PolicyScan(client, _ =>
                {
                    logAnalyticsClient.AddCustomLogJsonAsync("branchPolicy",
                        JsonConvert.SerializeObject(new
                        {
                            report = _,
                            Date = DateTime.UtcNow,
                        }), "Date");
                });
                scan.Execute("TAS");
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Failed to write branch policies to log analytics: {ex}");
                throw;
            }
        }
    }
}