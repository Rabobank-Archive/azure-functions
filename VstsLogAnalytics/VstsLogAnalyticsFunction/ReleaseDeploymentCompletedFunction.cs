using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SecurePipelineScan.VstsService;
using System;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using SecurePipelineScan.Rules;
using SecurePipelineScan.Rules.Events;
using SecurePipelineScan.Rules.Reports;
using VstsLogAnalytics.Client;
using VstsLogAnalytics.Common;

namespace VstsLogAnalyticsFunction
{
    public static class ReleaseDeploymentCompletedFunction
    {
        [FunctionName(nameof(ReleaseDeploymentCompletedFunction))]
        public static async System.Threading.Tasks.Task Run(
            [QueueTrigger("releasedeploymentcompleted", Connection = "connectionString")]string releaseCompleted,
            [Inject] ILogAnalyticsClient logAnalyticsClient,
            [Inject] IServiceHookScan<ReleaseDeploymentCompletedReport> scan,
            ILogger log)
        {
            if (logAnalyticsClient == null) throw new ArgumentNullException(nameof(logAnalyticsClient));
            if (scan == null) throw new ArgumentNullException(nameof(scan));

            log.LogInformation($"Queuetriggered {nameof(ReleaseDeploymentCompletedFunction)} by Azure Storage queue");
            log.LogInformation($"release: {releaseCompleted}");

            var report = scan.Completed(JObject.Parse(releaseCompleted));
            
            log.LogInformation("Done retrieving deployment information. Send to log analytics");
            await logAnalyticsClient.AddCustomLogJsonAsync("DeploymentStatus", report, "Date");
        }
    }
}