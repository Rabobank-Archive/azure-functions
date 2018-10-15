using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Rules.Reports;
using SecurePipelineScan.Rules;
using SecurePipelineScan.VstsService;
using System;
using System.Collections.Generic;
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
            [Inject]IVstsRestClient client,
            ILogger log)
        {
            try
            {
                log.LogInformation($"Branch Policies timed check start: {DateTime.Now}");

                var scan = new PolicyScan(client, _ =>
                {
                    var reports = _ as IEnumerable<BranchPolicyReport>;
                    foreach (var r in reports)
                    {
                        logAnalyticsClient.AddCustomLogJsonAsync("branchPolicy",
                            JsonConvert.SerializeObject(new
                            {
                                r.Project,
                                r.Repository,
                                r.HasRequiredReviewerPolicy,
                                Date = DateTime.UtcNow,

                            }), "Date");
                    }
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