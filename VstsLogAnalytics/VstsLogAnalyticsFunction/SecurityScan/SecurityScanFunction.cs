
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;

namespace VstsLogAnalyticsFunction
{
    public class SecurityScanFunction
    {

        private readonly IVstsRestClient _azuredo;

        public SecurityScanFunction(IVstsRestClient azuredo)
        {
            _azuredo = azuredo;
        }

        [FunctionName(nameof(SecurityScanFunction))]
        public async Task Run(
            [TimerTrigger("0 17 3 * * *", RunOnStartup=false)]
            TimerInfo timerInfo,
            [OrchestrationClient] DurableOrchestrationClientBase orchestrationClientBase,
            ILogger log)
        {
            var projects = _azuredo.Get(Project.Projects()).Value;

            var instanceId = await orchestrationClientBase.StartNewAsync(nameof(SecurityScanProjectOrchestration), projects);
            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
        }
    }
}