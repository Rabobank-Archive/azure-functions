using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureDevOps.Compliance.Rules;
using Functions.Activities;
using Functions.Helpers;
using Functions.Model;
using Functions.Starters;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Response = SecurePipelineScan.VstsService.Response;

namespace Functions.Orchestrators
{
    public class BuildPipelinesOrchestrator
    {
        private readonly EnvironmentConfig _config;

        public BuildPipelinesOrchestrator(EnvironmentConfig config) => _config = config;

        [FunctionName(nameof(BuildPipelinesOrchestrator))]
        public async Task RunAsync(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var (project, scanDate) = 
                context.GetInput<(Response.Project, DateTime)>();
            
            var buildPipelines = await context.CallActivityWithRetryAsync<List<Response.BuildDefinition>>(
                nameof(GetBuildPipelinesActivity), RetryHelper.ActivityRetryOptions, project.Id);

            var data = new ItemsExtensionData
            {
                Id = project.Name,
                Date = context.CurrentUtcDateTime,
                RescanUrl = ProjectScanHttpStarter.RescanUrl(_config, project.Name,
                    RuleScopes.BuildPipelines),
                HasReconcilePermissionUrl = ReconcileFunction.HasReconcilePermissionUrl(_config,
                    project.Id),
                Reports = await Task.WhenAll(buildPipelines.Select(b =>
                    StartScanActivityAsync(context, b, project)))
            };

            await context.CallActivityAsync(nameof(UploadExtensionDataActivity),
                (buildPipelines: data, RuleScopes.BuildPipelines));
        }

        private static Task<ItemExtensionData> StartScanActivityAsync(IDurableOrchestrationContext context, 
            Response.BuildDefinition b, Response.Project project) => 
            context.CallActivityWithRetryAsync<ItemExtensionData>(nameof(ScanBuildPipelinesActivity),
                RetryHelper.ActivityRetryOptions, (project, b));
    }
}