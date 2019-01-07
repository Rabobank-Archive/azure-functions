using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;
using VstsLogAnalytics.Common;
using VstsLogAnalyticsFunction.SecurityScan.Activites;

namespace VstsLogAnalyticsFunction.SecurityScan.Orchestrations
{
    public static class GetAllProjectTasks
    
    {
        [FunctionName(nameof(GetAllProjectTasks))]
        public static async Task<string> Run(
            [OrchestrationTrigger] DurableOrchestrationContextBase context,
            [Inject] IVstsRestClient client,
            ILogger log
        )

        {
            var projects = client.Get(Project.Projects()).Value;

            log.LogInformation($"Creating tasks for every project");
            var tasks = projects.Select(x => context.CallActivityAsync<int>(nameof(CreateSecurityReport), x));

            var enumerable = tasks.ToList();
            await Task.WhenAll(enumerable);

            return enumerable.Sum(t => t.Result).ToString();
        }
    }
}