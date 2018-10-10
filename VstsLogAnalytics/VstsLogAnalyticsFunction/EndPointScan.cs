using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SecurePipelineScan.VstsService;
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

            EndPointReport report = new EndPointReport();

            var endPointScan = new SecurePipelineScan.Rules.EndPointScan(client, (_) => report.Reports.Add(_));
            endPointScan.Execute(team);

            log.LogInformation("Done retrieving endpoint information. Send to log analytics");

            await logAnalyticsClient.AddCustomLogJsonAsync("EndpointScan", JsonConvert.SerializeObject(report), "Date");
        }
    }
}