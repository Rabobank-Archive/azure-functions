using AzDoCompliancy.CustomStatus;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.VstsService.Response;
using System.Collections.Generic;
using System.Linq;
using Functions.Helpers;
using Task = System.Threading.Tasks.Task;
using System.Threading;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Functions.Orchestrators
{
    public class ProjectScanSupervisor
    {
        private const int TimerInterval = 25; 

        [FunctionName(nameof(ProjectScanSupervisor))]
        public async Task RunAsync([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var projects = context.GetInput<List<Project>>();
            context.SetCustomStatus(new SupervisorOrchestrationStatus { TotalProjectCount = projects.Count });

            await Task.WhenAll(projects.Select(async (p, i) => 
                await StartProjectScanOrchestratorWithTimerAsync(context, p, i)));
        }

        private static async Task StartProjectScanOrchestratorWithTimerAsync(
            IDurableOrchestrationContext context, Project project, int index)
        {
            await context.CreateTimer(context.CurrentUtcDateTime.AddSeconds(index * TimerInterval), CancellationToken.None);
            await context.CallSubOrchestratorAsync(nameof(ProjectScanOrchestrator),
                OrchestrationHelper.CreateProjectScanOrchestrationId(context.InstanceId, project.Id),
                (project, (string)null));
        }
    }
}