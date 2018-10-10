using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SecurePipelineScan.VstsService;
using System;
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

            var endPointScan = new SecurePipelineScan.Rules.EndPointScan(client,
                (_) =>
                {
                    logAnalyticsClient.AddCustomLogJsonAsync("EndpointScan",
                        JsonConvert.SerializeObject(new
                        {
                            report = _,
                            Date = DateTime.UtcNow,
                        }), "Date");
                });

            endPointScan.Execute(team);

            log.LogInformation("Done retrieving endpoint information. Send to log analytics");
        }
    }
}