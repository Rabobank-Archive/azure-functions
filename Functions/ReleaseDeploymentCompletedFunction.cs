using Functions.Helpers;
using Functions.Model;
using LogAnalytics.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SecurePipelineScan.Rules.Events;
using SecurePipelineScan.Rules.Reports;
using SecurePipelineScan.VstsService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Requests = SecurePipelineScan.VstsService.Requests;

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
        public Task RunAsync(
            [QueueTrigger("releasedeploymentcompleted", Connection = "eventQueueStorageConnectionString")]string data,
            ILogger log)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (log == null)
                throw new ArgumentNullException(nameof(log));

            return RunInternalAsync(data);
        }

        private async Task RunInternalAsync(string data)
        {
            var report = await _scan.GetCompletedReportAsync(JObject.Parse(data));
            await _client.AddCustomLogJsonAsync("DeploymentStatus", report, "Date");
            await RetryHelper.ExecuteInvalidDocumentVersionPolicyAsync(_config.Organization, () => UpdateExtensionDataAsync(report));
        }

        private async Task UpdateExtensionDataAsync(ReleaseDeploymentCompletedReport report)
        {
            var reports = 
                await _azuredo.GetAsync(Requests.ExtensionManagement.ExtensionData<ExtensionDataReports<ReleaseDeploymentCompletedReport>>(
                    "tas", _config.ExtensionName, "Releases", report.Project)) ??
                new ExtensionDataReports<ReleaseDeploymentCompletedReport>
                {
                    Id = report.Project,
                    Reports = new List<ReleaseDeploymentCompletedReport>()
                };

            reports.Reports = reports
                .Reports
                .Concat(new[] { report })
                .OrderByDescending(x => x.CreatedDate)
                .Take(50)
                .ToList();

            await _azuredo.PutAsync(Requests.ExtensionManagement.ExtensionData<ExtensionDataReports<ReleaseDeploymentCompletedReport>>(
                "tas", _config.ExtensionName, "Releases", report.Project), reports);
        }
    }
}