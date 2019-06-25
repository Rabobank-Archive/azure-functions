using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;

namespace Functions.GlobalPermissionsScan
{
    public class GlobalPermissionsScanStarter
    {

        private readonly IVstsRestClient _azuredo;

        public GlobalPermissionsScanStarter(IVstsRestClient azuredo)
        {
            _azuredo = azuredo;
        }

        [FunctionName(nameof(GlobalPermissionsScanStarter))]
        public async Task Run(
            [TimerTrigger("0 17 3 * * *", RunOnStartup=false)]
            TimerInfo timerInfo,
            [OrchestrationClient] DurableOrchestrationClientBase orchestrationClientBase)
        {
            var projects = _azuredo.Get(Project.Projects()).ToList();

            await Task.WhenAll(projects.Select(p =>
                orchestrationClientBase.StartNewAsync(nameof(GlobalPermissionsScanProjectOrchestration), p)));
        }
    }
}