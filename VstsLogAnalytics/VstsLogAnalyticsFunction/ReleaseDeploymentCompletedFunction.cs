using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SecurePipelineScan.VstsService;
using System;
using Newtonsoft.Json.Linq;
using SecurePipelineScan.Rules.Events;
using SecurePipelineScan.Rules.Reports;
using VstsLogAnalytics.Client;
using System.Collections.Generic;
using System.Linq;

namespace VstsLogAnalyticsFunction
{
    public class ReleaseDeploymentCompletedFunction
    {
        private readonly ILogAnalyticsClient _client;
        private readonly IServiceHookScan<ReleaseDeploymentCompletedReport> _scan;
        private readonly IVstsRestClient _azuredo;

        public ReleaseDeploymentCompletedFunction(ILogAnalyticsClient client,
            IServiceHookScan<ReleaseDeploymentCompletedReport> scan,
            IVstsRestClient azuredo)
        {
            _client = client;
            _scan = scan;
            _azuredo = azuredo;
        }


        [FunctionName(nameof(ReleaseDeploymentCompletedFunction))]
        public async System.Threading.Tasks.Task Run(
            [QueueTrigger("releasedeploymentcompleted", Connection = "connectionString")]string releaseCompleted,
            ILogger log)
        {

            log.LogInformation($"Queuetriggered {nameof(ReleaseDeploymentCompletedFunction)} by Azure Storage queue");
            log.LogInformation($"release: {releaseCompleted}");

            var report = _scan.Completed(JObject.Parse(releaseCompleted));

            var releaseReports = _azuredo.Get(
                    SecurePipelineScan.VstsService.Requests.ExtensionManagement.ExtensionData<ExtensionDataReports<ReleaseDeploymentCompletedReport>>("tas", "tas",
            "Releases",report.Project));

            var releases = new List<ReleaseDeploymentCompletedReport>{ report };

            if (releaseReports != null && releaseReports.Reports != null)
            {
                foreach (var release in releaseReports.Reports)
                {
                    if (release.CreatedDate < new DateTime(2019, 4, 3))
                    {
                        release.UsesManagedAgentsOnly = null;
                    }
                }
                releases.AddRange(releaseReports.Reports.Take(49));
            }

            log.LogInformation($"Add release information to Azure DevOps Compliancy logging: {report.Project}");
            _azuredo.Put(
                SecurePipelineScan.VstsService.Requests.ExtensionManagement.ExtensionData<ExtensionDataReports<ReleaseDeploymentCompletedReport>>("tas", "tas",
                    "Releases"), new ExtensionDataReports<ReleaseDeploymentCompletedReport> { Reports = releases, Id = report.Project });

            log.LogInformation("Done retrieving deployment information. Send to log analytics");
            await _client.AddCustomLogJsonAsync("DeploymentStatus", report, "Date");
        }
    }
}