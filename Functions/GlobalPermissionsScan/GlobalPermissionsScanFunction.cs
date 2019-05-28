using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;

namespace Functions.GlobalPermissionsScan
{
    public class GlobalPermissionsScanFunction
    {

        private readonly IVstsRestClient _azuredo;

        public GlobalPermissionsScanFunction(IVstsRestClient azuredo)
        {
            _azuredo = azuredo;
        }

        [FunctionName(nameof(GlobalPermissionsScanFunction))]
        public async Task Run(
            [TimerTrigger("0 17 3 * * *", RunOnStartup=false)]
            TimerInfo timerInfo,
            [OrchestrationClient] DurableOrchestrationClientBase orchestrationClientBase,
            ILogger log)
        {
            var projects = _azuredo.Get(Project.Projects());

            var instanceId = await orchestrationClientBase.StartNewAsync(nameof(GlobalPermissionsScanProjectOrchestration), projects);
            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
        }
    }
}