using Functions.Activities;
using Functions.Helpers;
using Functions.Model;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService.Response;
using System;
using Task = System.Threading.Tasks.Task;

namespace Functions.Orchestrators
{
    public class ProjectScanOrchestration
    {
        [FunctionName(nameof(ProjectScanOrchestration))]
        public async Task RunAsync([OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var project = context.GetInput<Project>();

            var request = await context.CallActivityWithRetryAsync<ItemOrchestratorRequest>(
                nameof(LinkCisToReleasePipelinesActivity), RetryHelper.ActivityRetryOptions, project);

            await context.CallSubOrchestratorAsync(nameof(GlobalPermissionsOrchestration),
                OrchestrationIdHelper.CreateProjectScanScopeOrchestrationId(context.InstanceId,
                RuleScopes.GlobalPermissions), request);

            var buildRequest = 
                await context.CallSubOrchestratorAsync<ItemOrchestratorRequest>(
                nameof(ReleasePipelinesOrchestration), OrchestrationIdHelper.CreateProjectScanScopeOrchestrationId(
                context.InstanceId, RuleScopes.ReleasePipelines), request);

            var repositoryRequest =
                await context.CallSubOrchestratorAsync<ItemOrchestratorRequest>(
                nameof(BuildPipelinesOrchestration), OrchestrationIdHelper.CreateProjectScanScopeOrchestrationId(
                context.InstanceId, RuleScopes.BuildPipelines), buildRequest);

            await context.CallSubOrchestratorAsync(nameof(RepositoriesOrchestration),
                OrchestrationIdHelper.CreateProjectScanScopeOrchestrationId(context.InstanceId,
                RuleScopes.Repositories), repositoryRequest);
        }
    }
}