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
            List<LogAnalyticsReleaseItem> logAnalyticsItems = new List<LogAnalyticsReleaseItem>();

            var endPointScan = new SecurePipelineScan.Rules.EndPointScan(client,
                (_) =>
                {
                    ReleaseReport r = (ReleaseReport)_;
                    logAnalyticsItems.Add(new LogAnalyticsReleaseItem
                    {
                        Endpoint = _.Endpoint.Name,
                        Definition = _.Request.Definition.Name,
                        RequestId = _.Request.Id,
                        OwnerName = _.Request.Owner.Name,
                        HasFourEyesOnAllBuildArtefacts = (_ as ReleaseReport)?.Result,
                        Date = DateTime.UtcNow,
                    });
                });
            endPointScan.Execute(team);

            for (int i = 0; i < logAnalyticsItems.Count; i = i + 50)
            {
                var items = logAnalyticsItems.Skip(i).Take(50);
                await logAnalyticsClient.AddCustomLogJsonAsync("Release", JsonConvert.SerializeObject(items), "Date");
            }
            

            log.LogInformation("Done retrieving endpoint information. Send to log analytics");
        }
    }
}