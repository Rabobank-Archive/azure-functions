using Functions.Helpers;
using Functions.Model;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.VstsService.Response;
using Task = System.Threading.Tasks.Task;

namespace Functions.Orchestrators
{
    public class ProjectScanOrchestration

    {
        [FunctionName(nameof(ProjectScanOrchestration))]
        public async Task Run([OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            var project = context.GetInput<Project>();
            
            await context.CallSubOrchestratorAsync(nameof(GlobalPermissionsOrchestration),
                OrchestrationIdHelper.CreateProjectScanScopeOrchestrationId(context.InstanceId,
                    RuleScopes.GlobalPermissions), project);
            
            await context.CallSubOrchestratorAsync(nameof(RepositoriesOrchestration),
                OrchestrationIdHelper.CreateProjectScanScopeOrchestrationId(context.InstanceId,
                RuleScopes.Repositories), project);
            
            await context.CallSubOrchestratorAsync(nameof(BuildPipelinesOrchestration),
                OrchestrationIdHelper.CreateProjectScanScopeOrchestrationId(context.InstanceId,
                RuleScopes.BuildPipelines), project);
            
            await context.CallSubOrchestratorAsync(nameof(ReleasePipelinesOrchestration),
                OrchestrationIdHelper.CreateProjectScanScopeOrchestrationId(context.InstanceId,
                RuleScopes.ReleasePipelines), project);
        }
    }
}