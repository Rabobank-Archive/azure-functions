using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SecurePipelineScan.VstsService;
using System;
using VstsLogAnalytics.Common;
using Requests = SecurePipelineScan.VstsService.Requests;

namespace VstsLogAnalyticsFunction
{
    public class RepositoryScanFunction
    {
        private readonly IVstsRestClient _azuredo;

        public RepositoryScanFunction(IVstsRestClient azuredo)
        {
            _azuredo = azuredo;
        }


        [FunctionName(nameof(RepositoryScanFunction))]
        public async System.Threading.Tasks.Task Run(
            [TimerTrigger("0 0 1 * * *", RunOnStartup=false)] TimerInfo timerInfo,
            [OrchestrationClient] DurableOrchestrationClientBase orchestrationClientBase,
            ILogger log)
        {
            if (orchestrationClientBase == null) { throw new ArgumentNullException(nameof(orchestrationClientBase)); }

            try
            {
                log.LogInformation($"Repository scan timed check start: {DateTime.Now}");

                var projects = _azuredo.Get(Requests.Project.Projects());
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