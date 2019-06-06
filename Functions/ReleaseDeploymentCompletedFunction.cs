using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SecurePipelineScan.VstsService;
using System;
using Newtonsoft.Json.Linq;
using SecurePipelineScan.Rules.Events;
using SecurePipelineScan.Rules.Reports;
using LogAnalytics.Client;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Requests = SecurePipelineScan.VstsService.Requests;
using Functions.Helpers;

namespace Functions
{
    public class ReleaseDeploymentCompletedFunction
    {
        private readonly ILogAnalyticsClient _client;
        private readonly IServiceHookScan<ReleaseDeploymentCompletedReport> _scan;
        private readonly IVstsRestClient _azuredo;
        private readonly EnvironmentConfig _config;

        public ReleaseDeploymentCompletedFunction(
            ILogAnalyticsClient client,
            IServiceHookScan<ReleaseDeploymentCompletedReport> scan,
            IVstsRestClient azuredo, 
            EnvironmentConfig config)
        {
            _client = client;
            _scan = scan;
            _azuredo = azuredo;
            _config = config;
        }


        [FunctionName(nameof(ReleaseDeploymentCompletedFunction))]
        public async Task Run(
            [QueueTrigger("releasedeploymentcompleted", Connection = "connectionString")]string data,
            ILogger log)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (log == null) throw new ArgumentNullException(nameof(log));

            var report = _scan.Completed(JObject.Parse(data));
            await _client.AddCustomLogJsonAsync("DeploymentStatus", report, "Date");
            RetryHelper.InvalidDocumentVersionPolicy.Execute(() => UpdateExtensionData(report));
        }

        private void UpdateExtensionData(ReleaseDeploymentCompletedReport report)
        {
            var reports = _azuredo.Get(
                                     Requests.ExtensionManagement
                                         .ExtensionData<ExtensionDataReports<ReleaseDeploymentCompletedReport>>(
                                             "tas",
                                             _config.ExtensionName,

                                             "Releases",
                                             report.Project)) ??
                                 new ExtensionDataReports<ReleaseDeploymentCompletedReport>
                                 {
                                     Id = report.Project, Reports = new List<ReleaseDeploymentCompletedReport>()
                                 };

            reports.Reports = reports
                .Reports
                .Concat(new[]{report})
                .OrderByDescending(x => x.CreatedDate)
                .Take(50)
                .ToList();

            _azuredo.Put(
                Requests.ExtensionManagement.ExtensionData<ExtensionDataReports<ReleaseDeploymentCompletedReport>>(
                    "tas", _config.ExtensionName,
                    "Releases", report.Project), reports);
        }
    }
}