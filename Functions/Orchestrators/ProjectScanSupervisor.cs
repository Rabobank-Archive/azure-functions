using Microsoft.Azure.WebJobs;
using SecurePipelineScan.VstsService.Response;
using System.Collections.Generic;
using System.Linq;
using Functions.Helpers;
using Task = System.Threading.Tasks.Task;
using System.Threading;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System;

namespace Functions.Orchestrators
{
    public class ProjectScanSupervisor
    {
        private const int TimerInterval = 25; 

        [FunctionName(nameof(ProjectScanSupervisor))]
        public async Task RunAsync([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var scanDate = context.CurrentUtcDateTime;
            var projects = context.GetInput<List<Project>>();

            await Task.WhenAll(projects.Select(async (p, i) => 
                await StartProjectScanOrchestratorWithTimerAsync(context, p, i, scanDate)));
        }

        private static async Task StartProjectScanOrchestratorWithTimerAsync(
            IDurableOrchestrationContext context, Project project, int index, DateTime scanDate)
        {
            await context.CreateTimer(context.CurrentUtcDateTime.AddSeconds(index * TimerInterval), CancellationToken.None);
            await context.CallSubOrchestratorAsync(nameof(ProjectScanOrchestrator),
                (project, (string)null, scanDate));
        }
    }
}