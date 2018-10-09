using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SecurePipelineScan.Rules.Reports;
using SecurePipelineScan.VstsService;
using System.Collections.Generic;
using System.Threading.Tasks;
using VstsLogAnalytics.Client;
using VstsLogAnalytics.Common;

namespace VstsLogAnalyticsFunction
{
    public static class EndPointScan
    {
        [FunctionName("EndPointScan")]
        public static async Task Run(
            [TimerTrigger("0 0 */4 * * *")] TimerInfo timerInfo,
            [Inject]ILogAnalyticsClient logAnalyticsClient,
            [Inject] IVstsRestClient client,
            ILogger log)
        {
            log.LogInformation("C# Time trigger function to process endpoints");

            string team = "TAS";

            List<ScanReport> scanReports = new List<ScanReport>();

            var endPointScan = new SecurePipelineScan.Rules.EndPointScan(client, (_) => scanReports.Add(_));
            endPointScan.Execute(team);

            await logAnalyticsClient.AddCustomLogJsonAsync("EndpointScan", JsonConvert.SerializeObject(scanReports), "Date");
        }
    }
}