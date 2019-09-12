using Functions.Activities;
using Functions.Helpers;
using Functions.Model;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage.Table;
using SecurePipelineScan.VstsService.Response;
using Task = System.Threading.Tasks.Task;

namespace Functions.Orchestrators
{
    public class ProjectScanOrchestration
    {
        [FunctionName(nameof(ProjectScanOrchestration))]
        public async Task RunAsync([OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            var project = context.GetInput<Project>();

            var releaseOrchestratorRequest = await context.CallActivityWithRetryAsync<ItemOrchestratorRequest>(
                nameof(GetDataFromTableStorageActivity), RetryHelper.ActivityRetryOptions, project);

            await context.CallSubOrchestratorAsync(nameof(GlobalPermissionsOrchestration),
                OrchestrationIdHelper.CreateProjectScanScopeOrchestrationId(context.InstanceId,
                    RuleScopes.GlobalPermissions), releaseOrchestratorRequest);
            
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