using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SecurePipelineScan.Rules.Events;
using SecurePipelineScan.Rules.Reports;
using VstsLogAnalytics.Client;
using VstsLogAnalytics.Common;

namespace VstsLogAnalyticsFunction
{
    public class BuildCompleted
    {
        [FunctionName(nameof(BuildCompleted))]
        public static void Run(
            [QueueTrigger("buildcompleted", Connection = "connectionString")]string data,
            [Inject] ILogAnalyticsClient client,
            [Inject] IServiceHookScan<BuildScanReport> scan,
            ILogger log)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (scan == null) throw new ArgumentNullException(nameof(scan));
            if (log == null) throw new ArgumentNullException(nameof(log));
            
            var report = scan.Completed(JObject.Parse(data));
            client.AddCustomLogJsonAsync(nameof(BuildCompleted), report, "Date");
        }
    }
}