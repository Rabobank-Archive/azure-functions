using AzDoCompliancy.CustomStatus;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.VstsService.Response;
using System.Collections.Generic;
using System.Linq;
using Task = System.Threading.Tasks.Task;

namespace Functions.Orchestrators
{
    public class ProjectScanSupervisor
    {
        [FunctionName(nameof(ProjectScanSupervisor))]
        public async Task Run([OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            var projects = context.GetInput<List<Project>>();
            context.SetCustomStatus(new SupervisorOrchestrationStatus { TotalProjectCount = projects.Count });
            await Task.WhenAll(projects.Select(p => 
                context.CallSubOrchestratorAsync(nameof(ProjectScanOrchestration), p)));
        }
    }
}
