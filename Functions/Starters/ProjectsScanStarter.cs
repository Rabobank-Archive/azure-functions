using System.Linq;
using System.Threading.Tasks;
using Functions.Orchestrators;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;

namespace Functions.Starters
{
    public class ProjectsScanStarter
    {

        private readonly IVstsRestClient _azuredo;

        public ProjectsScanStarter(IVstsRestClient azuredo)
        {
            _azuredo = azuredo;
        }

        [FunctionName(nameof(ProjectsScanStarter))]
        public async Task Run(
            [TimerTrigger("0 17 3 * * *", RunOnStartup=false)]
            TimerInfo timerInfo,
            [OrchestrationClient] DurableOrchestrationClientBase orchestrationClientBase)
        {
            var projects = _azuredo.Get(Project.Projects()).Select(p => p.Name).ToList();

            await Task.WhenAll(projects.Select(p =>
                orchestrationClientBase.StartNewAsync(nameof(ProjectScanOrchestration), p)));
        }
    }
}