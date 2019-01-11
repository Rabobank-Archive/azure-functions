using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;
using VstsLogAnalytics.Common;
using VstsLogAnalyticsFunction.SecurityScan.Orchestrations;

namespace VstsLogAnalyticsFunction.SecurityScan
{
    public static class SecurityScanStart
    {
        [FunctionName(nameof(SecurityScanStart))]
        public static async Task Run(
            [TimerTrigger("0 0 6 * * *", RunOnStartup = true)]
            TimerInfo timerInfo,
            [OrchestrationClient] DurableOrchestrationClientBase orchestrationClientBase,
            [Inject] IVstsRestClient client,
            ILogger log)
        {
            var projects = client.Get(Project.Projects()).Value;

            var instanceId = await orchestrationClientBase.StartNewAsync(nameof(GetAllProjectTasks), projects);
            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
        }
    }
}