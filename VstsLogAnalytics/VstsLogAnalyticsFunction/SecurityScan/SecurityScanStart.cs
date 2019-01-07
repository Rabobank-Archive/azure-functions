using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
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
            ILogger log)
        {
            var instanceId = await orchestrationClientBase.StartNewAsync(nameof(GetAllProjectTasks), null);
            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
        }
    }
}