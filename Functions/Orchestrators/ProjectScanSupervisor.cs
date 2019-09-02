using AzDoCompliancy.CustomStatus;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.VstsService.Response;
using System.Collections.Generic;
using System.Linq;
using Functions.Helpers;
using Task = System.Threading.Tasks.Task;

namespace Functions.Orchestrators
{
    public class ProjectScanSupervisor
    {
        [FunctionName(nameof(ProjectScanSupervisor))]
        public async Task RunAsync([OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            var projects = context.GetInput<List<Project>>();
            context.SetCustomStatus(new SupervisorOrchestrationStatus { TotalProjectCount = projects.Count });

            //No multithreading with linq, so we won't hit rate limits
            foreach (var project in projects)
            {
                await context.CallSubOrchestratorAsync(nameof(ProjectScanOrchestration),
                    OrchestrationIdHelper.CreateProjectScanOrchestrationId(context.InstanceId, project.Id), project);
            }
        }
    }
}
