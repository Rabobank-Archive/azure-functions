using Microsoft.Azure.WebJobs;
using Task = System.Threading.Tasks.Task;

namespace Functions.Orchestrators
{
    public class ProjectScanOrchestration
    
    {
        [FunctionName(nameof(ProjectScanOrchestration))]
        public async Task Run([OrchestrationTrigger] DurableOrchestrationContextBase context)
        {   
            var project = context.GetInput<string>();
            await context.CallSubOrchestratorAsync(nameof(GlobalPermissionsOrchestration), project);
            await context.CallSubOrchestratorAsync(nameof(BuildPipelinesOrchestration), project);
            await context.CallSubOrchestratorAsync(nameof(ReleasePipelinesOrchestration), project);
        }
    }
}