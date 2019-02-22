using System;
using System.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SecurePipelineScan.Rules.Events;
using SecurePipelineScan.Rules.Reports;
using SecurePipelineScan.VstsService;
using VstsLogAnalytics.Client;
using VstsLogAnalytics.Common;
using Requests = SecurePipelineScan.VstsService.Requests;

namespace VstsLogAnalyticsFunction
{
    public class BuildCompleted
    {
        [FunctionName(nameof(BuildCompleted))]
        public static void Run([QueueTrigger("buildcompleted", Connection = "connectionString")]
            string data,
            [Inject] ILogAnalyticsClient client,
            [Inject] IServiceHookScan<BuildScanReport> scan,
            IVstsRestClient azuredo,
            ILogger log)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (scan == null) throw new ArgumentNullException(nameof(scan));
            if (azuredo == null) throw new ArgumentNullException(nameof(azuredo));
            if (log == null) throw new ArgumentNullException(nameof(log));
            
            var report = scan.Completed(JObject.Parse(data));
            client.AddCustomLogJsonAsync(nameof(BuildCompleted), report, "Date");
            UpdateExtensionData(azuredo, report);
        }

        private static void UpdateExtensionData(IVstsRestClient azuredo, BuildScanReport report)
        {
            var reports = azuredo.Get(
                Requests.ExtensionManagement.ExtensionData<ExtensionDataReports<BuildScanReport>>(
                    "tas", 
                    "tas",
                    "BuildReports", 
                    report.Project));

            if (reports == null)
            {
                reports = new ExtensionDataReports<BuildScanReport>() {Id = report.Project, Reports = new[] {report}};
            }
            else
            {
                reports.Reports.Insert(0, report);
                reports.Reports = reports.Reports.Take(50).ToList();
            }
                        
            azuredo.Put(
                Requests.ExtensionManagement.ExtensionData<ExtensionDataReports<BuildScanReport>>("tas", "tas",
                    "BuildReports", report.Project), reports);
        }
    }
}