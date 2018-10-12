using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SecurePipelineScan.Rules;
using SecurePipelineScan.Rules.Reports;
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

                ScanReportWrapper reportWrapper = new ScanReportWrapper();

                var scan = new PolicyScan(client, _ => reportWrapper.ScanReport = _);
                scan.Execute("TAS");

                await logAnalyticsClient.AddCustomLogJsonAsync("branchPolicy", JsonConvert.SerializeObject(reportWrapper), "Date");

                log.LogInformation($"Branch Policies timed check end: {DateTime.UtcNow}");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to write branch policies to log analytics");
            }
        }

        /// <summary>
        /// A small wrapper to have a datetime for loganalytics.
        /// </summary>
        private class ScanReportWrapper
        {
            public ScanReport ScanReport { get; set; }
            public DateTime Date { get; set; }

            public ScanReportWrapper()
            {
                Date = DateTime.UtcNow;
            }
        }
    }
}