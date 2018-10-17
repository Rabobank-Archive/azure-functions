using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SecurePipelineScan.Rules.Reports;
using SecurePipelineScan.VstsService;
using System;
using System.Collections.Generic;
using System.Linq;
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

            List<LogAnalyticsReleaseItem> list = new List<LogAnalyticsReleaseItem>();

            var endPointScan = new SecurePipelineScan.Rules.EndPointScan(client);
            var results = endPointScan.Execute(team);

            foreach (var report in results.OfType<ReleaseReport>())
            {
                list.Add(
                    new LogAnalyticsReleaseItem
                    {
                        Endpoint = report.Endpoint.Name,
                        EndpointType = report.Endpoint.Type,
                        Definition = report.Request.Definition.Name,
                        RequestId = report.Request.Id,
                        StageName = report.Request.Owner.Name,
                        HasFourEyesOnAllBuildArtefacts = (report as ReleaseReport)?.Result,
                        Date = DateTime.UtcNow,
                    });
            }

            log.LogInformation("Done retrieving endpoint information. Send to log analytics");

            for (int i = 0; i < list.Count; i = i + 100)
            {
                var items = list.Skip(i).Take(100);

                await logAnalyticsClient.AddCustomLogJsonAsync("EndpointScan",
                    JsonConvert.SerializeObject(items), "Date");
            }
        }
    }
}