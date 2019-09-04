using AzDoCompliancy.CustomStatus;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.VstsService.Response;
using System.Collections.Generic;
using System.Linq;
using Functions.Helpers;
using Task = System.Threading.Tasks.Task;
using System.Threading;
using System;

namespace Functions.Orchestrators
{
    public class ProjectScanSupervisor
    {
        private const int MaxSleep = 300; 

        [FunctionName(nameof(ProjectScanSupervisor))]
        public async Task RunAsync([OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            var projects = context.GetInput<List<Project>>();
            context.SetCustomStatus(new SupervisorOrchestrationStatus { TotalProjectCount = projects.Count });

            await Task.WhenAll(projects.Select(async p => await StartProjectScanOrchestratorWithTimeTriggerAsync(context, p)));
                
            //No multithreading with linq, so we won't hit rate limits
            //foreach (var project in projects)
            //{
            //    await context.CallSubOrchestratorAsync(nameof(ProjectScanOrchestration),
            //        OrchestrationIdHelper.CreateProjectScanOrchestrationId(context.InstanceId, project.Id), project);
            //}
        }

        private async static Task StartProjectScanOrchestratorWithTimeTriggerAsync(DurableOrchestrationContextBase context, Project project)
        {
            await context.CreateTimer(DateTime.Now.AddMinutes(new Random().Next(MaxSleep)), CancellationToken.None);
            await context.CallSubOrchestratorAsync(nameof(ProjectScanOrchestration),
                OrchestrationIdHelper.CreateProjectScanOrchestrationId(context.InstanceId, project.Id), project);
        }
    }
}