using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SecurePipelineScan.Rules.Events;
using SecurePipelineScan.Rules.Reports;
using SecurePipelineScan.VstsService;
using LogAnalytics.Client;
using Requests = SecurePipelineScan.VstsService.Requests;

namespace Functions
{
    public class BuildCompletedFunction
    {
        private readonly ILogAnalyticsClient _client;
        private readonly IServiceHookScan<BuildScanReport> _scan;
        private readonly IVstsRestClient _azuredo;

        public BuildCompletedFunction(ILogAnalyticsClient client,
            IServiceHookScan<BuildScanReport> scan,
            IVstsRestClient azuredo)
        {
            _client = client;
            _scan = scan;
            _azuredo = azuredo;
        }

        [FunctionName(nameof(BuildCompletedFunction))]
        public void Run([QueueTrigger("buildcompleted", Connection = "connectionString")]
            string data, ILogger log)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (log == null) throw new ArgumentNullException(nameof(log));
            
            var report = _scan.Completed(JObject.Parse(data));
            _client.AddCustomLogJsonAsync(nameof(BuildCompletedFunction), report, "Date");
            UpdateExtensionData(report);
        }

        private void UpdateExtensionData(BuildScanReport report)
        {
            var reports = _azuredo.Get(
                Requests.ExtensionManagement.ExtensionData<ExtensionDataReports<BuildScanReport>>(
                    "tas", 
                    "tas",
                    "BuildReports", 
                    report.Project)) ?? new ExtensionDataReports<BuildScanReport> { Id = report.Project, Reports = new List<BuildScanReport>() };

            reports.Reports = reports
                .Reports
                .Concat(new[]{report})
                .OrderByDescending(x => x.CreatedDate)
                .Take(50)
                .ToList();

            _azuredo.Put(
                Requests.ExtensionManagement.ExtensionData<ExtensionDataReports<BuildScanReport>>("tas", "tas",
                    "BuildReports", report.Project), reports);
        }
    }
}