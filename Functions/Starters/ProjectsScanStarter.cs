using Functions.Orchestrators;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Functions.Starters
{
    public class ProjectsScanStarter
    {
        private readonly IVstsRestClient _azuredo;

        public ProjectsScanStarter(IVstsRestClient azuredo) => _azuredo = azuredo;

        [FunctionName(nameof(ProjectsScanStarter))]
        public async Task RunAsync(
            [TimerTrigger("0 0 20 * * *", RunOnStartup=false)] TimerInfo timerInfo,
            [OrchestrationClient] DurableOrchestrationClientBase orchestrationClientBase)
        {
            if (orchestrationClientBase == null)
                throw new ArgumentNullException(nameof(orchestrationClientBase));

            var projects = _azuredo.Get(Project.Projects()).ToList();
            await orchestrationClientBase.StartNewAsync(nameof(ProjectScanSupervisor), projects);
        }
    }
}