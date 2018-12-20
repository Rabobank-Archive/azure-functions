using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SecurePipelineScan.Rules.Release;
using SecurePipelineScan.VstsService;
using System;
using System.IO;
using Indigo.Functions.Injection;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using SecurePipelineScan.Rules;
using SecurePipelineScan.Rules.Events;
using VstsLogAnalytics.Client;
using VstsLogAnalytics.Common;
using Requests = SecurePipelineScan.VstsService.Requests;

namespace VstsLogAnalyticsFunction
{
    public static class ReleaseDeploymentCompletedFunction
    {
        [FunctionName(nameof(ReleaseDeploymentCompletedFunction))]
        public static async System.Threading.Tasks.Task Run(
            [QueueTrigger("releasedeploymentcompleted", Connection = "connectionString")]string releaseCompleted,
            [Inject] ILogAnalyticsClient logAnalyticsClient,
            [Inject] IVstsRestClient client,
            [Inject] IMemoryCache cache,
            ILogger log)
        {
            if (logAnalyticsClient == null) { throw new ArgumentNullException("Log Analytics Client is not set"); }
            if (client == null) { throw new ArgumentNullException("VSTS Rest client is not set"); }
            if (cache == null) { throw new ArgumentNullException("MemoryCache is not set"); }

            log.LogInformation($"Queuetriggered {nameof(ReleaseDeploymentCompletedFunction)} by Azure Storage queue");
            log.LogInformation($"release: {releaseCompleted}");

            var scan = new ReleaseDeploymentScan(new ServiceEndpointValidator(client, cache));
            var report = scan.Completed(JObject.Parse(releaseCompleted));
            
            log.LogInformation("Done retrieving deployment information. Send to log analytics");

            await logAnalyticsClient.AddCustomLogJsonAsync("DeploymentStatus", report, "Date");
        }
    }
}