using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzDoCompliancy.CustomStatus;
using Functions.Activities;
using Functions.Helpers;
using Functions.Model;
using Functions.Starters;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService.Response;
using Task = System.Threading.Tasks.Task;

namespace Functions.Orchestrators
{
    public class BuildPipelinesOrchestration
    {
        private readonly EnvironmentConfig _config;

        public BuildPipelinesOrchestration(EnvironmentConfig config)
        {
            _config = config;
        }

        [FunctionName(nameof(BuildPipelinesOrchestration))]
        public async Task<ItemOrchestratorRequest> RunAsync(
            [OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            var request = context.GetInput<ItemOrchestratorRequest>();

            context.SetCustomStatus(new ScanOrchestrationStatus
            {
                Project = request.Project.Name,
                Scope = RuleScopes.BuildPipelines
            });

            var buildPipelines = await context.CallActivityWithRetryAsync<List<BuildDefinition>>(
                nameof(GetBuildPipelinesActivity), RetryHelper.ActivityRetryOptions, request.Project.Id);

            var data = new ItemsExtensionData
            {
                Id = request.Project.Name,
                Date = context.CurrentUtcDateTime,
                RescanUrl = ProjectScanHttpStarter.RescanUrl(_config, request.Project.Name, 
                    RuleScopes.BuildPipelines),
                HasReconcilePermissionUrl = ReconcileFunction.HasReconcilePermissionUrl(_config, 
                    request.Project.Id),
                Reports = await Task.WhenAll(buildPipelines.Select(b =>
                    context.CallActivityWithRetryAsync<ItemExtensionData>(nameof(ScanBuildPipelinesActivity),
                    RetryHelper.ActivityRetryOptions, new BuildPipelinesScanActivityRequest
                    {
                        Project = request.Project,
                        BuildDefinition = b,
                        CiIdentifiers = request.ProductionItems
                            .Where(r => r.ItemId == b.Id)
                            .SelectMany(r => r.CiIdentifiers)
                            .ToList()
                    })))
            };

            await context.CallActivityAsync(nameof(UploadPreventiveRuleLogsActivity),
                data.Flatten(RuleScopes.BuildPipelines, context.InstanceId));

            await context.CallActivityAsync(nameof(UploadExtensionDataActivity),
                (buildPipelines: data, RuleScopes.BuildPipelines));

            return new ItemOrchestratorRequest
            {
                Project = request.Project,
                ProductionItems = (await Task.WhenAll(buildPipelines
                    .Where(r => request.ProductionItems.Select(p => p.ItemId).Contains(r.Id))
                    .Select(r => context.CallActivityAsync<ProductionItem>(
                        nameof(LinkCisToRepositoriesActivity), (r, request.ProductionItems.First(
                            p => p.ItemId == r.Id).CiIdentifiers, request.Project.Id)))))
                    .GroupBy(p => p.ItemId)
                    .Select(g => new ProductionItem
                    {
                        ItemId = g.Key,
                        CiIdentifiers = g
                            .SelectMany(p => p.CiIdentifiers)
                            .Distinct()
                            .ToList()
                    })
                    .ToList()
            };
        }
    }
}