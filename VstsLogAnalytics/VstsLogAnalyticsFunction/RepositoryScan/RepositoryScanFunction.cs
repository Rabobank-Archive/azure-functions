using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SecurePipelineScan.VstsService;
using System;
using VstsLogAnalytics.Common;
using Requests = SecurePipelineScan.VstsService.Requests;

namespace VstsLogAnalyticsFunction.RepositoryScan
{
    public static class RepositoryScanFunction
    {
        [FunctionName(nameof(RepositoryScanFunction))]
        public static async System.Threading.Tasks.Task Run(
            [TimerTrigger("0 */30 * * * *")] TimerInfo timerInfo,
            [Inject] IVstsRestClient client,
            [OrchestrationClient] DurableOrchestrationClientBase orchestrationClientBase,
            ILogger log)
        {
            if (client == null) { throw new ArgumentNullException(nameof(client)); }
            if (orchestrationClientBase == null) { throw new ArgumentNullException(nameof(orchestrationClientBase)); }

            try
            {
                log.LogInformation($"Repository scan timed check start: {DateTime.Now}");

                var projects = client.Get(Requests.Project.Projects());
                log.LogInformation($"Projects found: {projects.Count}");


                var instanceId = await orchestrationClientBase.StartNewAsync(nameof(RepositoryScanProjectOrchestration), projects);
                log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Error executing Repository Scan Orchestration");
                throw;
            }
        }
    }
}