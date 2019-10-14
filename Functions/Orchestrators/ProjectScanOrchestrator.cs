using Functions.Activities;
using Functions.Helpers;
using Functions.Model;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService.Response;
using System;
using System.Collections.Generic;
using Task = System.Threading.Tasks.Task;

namespace Functions.Orchestrators
{
    public class ProjectScanOrchestrator
    {
        [FunctionName(nameof(ProjectScanOrchestrator))]
        public async Task RunAsync([OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var project = context.GetInput<Project>();

            var releaseProductionItems = await context.CallActivityWithRetryAsync<IList<ProductionItem>>(
                nameof(GetDeploymentMethodsActivity), RetryHelper.ActivityRetryOptions, project);

            await context.CallSubOrchestratorAsync(nameof(GlobalPermissionsOrchestrator),
                OrchestrationHelper.CreateProjectScanScopeOrchestrationId(context.InstanceId,
                RuleScopes.GlobalPermissions), (project, releaseProductionItems));

            var buildProductionItems = 
                await context.CallSubOrchestratorAsync<IList<ProductionItem>>(
                nameof(ReleasePipelinesOrchestrator), OrchestrationHelper.CreateProjectScanScopeOrchestrationId(
                context.InstanceId, RuleScopes.ReleasePipelines), (project, releaseProductionItems));

            var repositoryProductionItems =
                await context.CallSubOrchestratorAsync<IList<ProductionItem>>(
                nameof(BuildPipelinesOrchestrator), OrchestrationHelper.CreateProjectScanScopeOrchestrationId(
                context.InstanceId, RuleScopes.BuildPipelines), (project, buildProductionItems));

            await context.CallSubOrchestratorAsync(nameof(RepositoriesOrchestrator),
                OrchestrationHelper.CreateProjectScanScopeOrchestrationId(context.InstanceId,
                RuleScopes.Repositories), (project, repositoryProductionItems));
        }
    }
}