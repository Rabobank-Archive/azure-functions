using System;
using System.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SecurePipelineScan.VstsService;
using Requests = SecurePipelineScan.VstsService.Requests;

namespace Functions.ItemScan
{
    public class ItemScanFunction
    {
        private readonly IVstsRestClient _azuredo;

        public ItemScanFunction(IVstsRestClient azuredo)
        {
            _azuredo = azuredo;
        }


        [FunctionName(nameof(ItemScanFunction))]
        public async System.Threading.Tasks.Task Run(
            [TimerTrigger("0 0 1 * * *", RunOnStartup=false)] TimerInfo timerInfo,
            [OrchestrationClient] DurableOrchestrationClientBase orchestrationClientBase,
            ILogger log)
        {
            if (orchestrationClientBase == null) { throw new ArgumentNullException(nameof(orchestrationClientBase)); }

            try
            {
                log.LogInformation($"Item scan timed check start: {DateTime.Now}");

                var projects = (_azuredo.Get(Requests.Project.Projects())).ToList();
                log.LogInformation($"Projects found: {projects.Count}");
                
                var instanceId = await orchestrationClientBase.StartNewAsync(nameof(ItemScanProjectOrchestration), projects);
                log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Error executing Item Scan Orchestration");
                throw;
            }
        }
    }
}