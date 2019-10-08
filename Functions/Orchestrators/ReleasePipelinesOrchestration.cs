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
using Response = SecurePipelineScan.VstsService.Response;

namespace Functions.Orchestrators
{
    public class ReleasePipelinesOrchestration
    {
        private readonly EnvironmentConfig _config;

        public ReleasePipelinesOrchestration(EnvironmentConfig config)
        {
            _config = config;
        }

        [FunctionName(nameof(ReleasePipelinesOrchestration))]
        public async Task<ItemOrchestratorRequest> RunAsync(
            [OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            var request = context.GetInput<ItemOrchestratorRequest>();

            context.SetCustomStatus(new ScanOrchestrationStatus
            {
                Project = request.Project.Name,
                Scope = RuleScopes.ReleasePipelines
            });

            var releasePipelines = 
                await context.CallActivityWithRetryAsync<List<Response.ReleaseDefinition>>(
                nameof(GetReleasePipelinesActivity), RetryHelper.ActivityRetryOptions, request.Project.Id);

            var data = new ItemsExtensionData
            {
                Id = request.Project.Name,
                Date = context.CurrentUtcDateTime,
                RescanUrl = ProjectScanHttpStarter.RescanUrl(
                    _config, request.Project.Name, RuleScopes.ReleasePipelines),
                HasReconcilePermissionUrl = ReconcileFunction.HasReconcilePermissionUrl(
                    _config, request.Project.Id),
                Reports = 
                    await Task.WhenAll(releasePipelines.Select(r =>
                    context.CallActivityWithRetryAsync<ItemExtensionData>(
                    nameof(ScanReleasePipelinesActivity), RetryHelper.ActivityRetryOptions, 
                    new ReleasePipelinesScanActivityRequest
                    {
                        Project = request.Project,
                        ReleaseDefinition = r,
                        CiIdentifiers = request.ProductionItems
                            .Where(p => p.ItemId == r.Id)
                            .SelectMany(p => p.CiIdentifiers)
                            .ToList()
                    })))
            };

            await context.CallActivityAsync(nameof(UploadPreventiveRuleLogsActivity),
                data.Flatten(RuleScopes.ReleasePipelines, context.InstanceId));

            await context.CallActivityAsync(nameof(UploadExtensionDataActivity),
                (releasePipelines: data, RuleScopes.ReleasePipelines));

            return new ItemOrchestratorRequest
            {
                Project = request.Project,
                ProductionItems = (await Task.WhenAll(releasePipelines
                    .Where(r => request.ProductionItems.Select(p => p.ItemId).Contains(r.Id))
                    .Select(r => context.CallActivityAsync<IList<ProductionItem>>(
                        nameof(LinkCisToBuildPipelinesActivity), (r, request.ProductionItems.First(
                            p => p.ItemId == r.Id).CiIdentifiers, request.Project.Id)))))
                    .SelectMany(p => p)
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