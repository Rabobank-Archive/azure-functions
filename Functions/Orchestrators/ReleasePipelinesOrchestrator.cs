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
    public class ReleasePipelinesOrchestrator
    {
        private readonly EnvironmentConfig _config;

        public ReleasePipelinesOrchestrator(EnvironmentConfig config) => _config = config;

        [FunctionName(nameof(ReleasePipelinesOrchestrator))]
        public async Task<IList<ProductionItem>> RunAsync(
            [OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            var (project, productionItems) = context.GetInput<(Response.Project, List<ProductionItem>)>();

            context.SetCustomStatus(new ScanOrchestrationStatus
            {
                Project = project.Name,
                Scope = RuleScopes.ReleasePipelines
            });

            var releasePipelines = 
                await context.CallActivityWithRetryAsync<List<Response.ReleaseDefinition>>(
                nameof(GetReleasePipelinesActivity), RetryHelper.ActivityRetryOptions, project.Id);

            var data = new ItemsExtensionData
            {
                Id = project.Name,
                Date = context.CurrentUtcDateTime,
                RescanUrl = ProjectScanHttpStarter.RescanUrl(
                    _config, project.Name, RuleScopes.ReleasePipelines),
                HasReconcilePermissionUrl = ReconcileFunction.HasReconcilePermissionUrl(
                    _config, project.Id),
                Reports = await Task.WhenAll(releasePipelines.Select(r =>
                    context.CallActivityWithRetryAsync<ItemExtensionData>(
                    nameof(ScanReleasePipelinesActivity), RetryHelper.ActivityRetryOptions, 
                    (project, r, productionItems
                        .Where(p => p.ItemId == r.Id)
                        .SelectMany(p => p.DeploymentInfo)
                        .ToList()))))
            };

            await context.CallActivityAsync(nameof(UploadPreventiveRuleLogsActivity),
                data.Flatten(RuleScopes.ReleasePipelines, context.InstanceId));

            await context.CallActivityAsync(nameof(UploadExtensionDataActivity),
                (releasePipelines: data, RuleScopes.ReleasePipelines));

            return LinkConfigurationItemHelper.LinkCisToBuildPipelines(releasePipelines,
                productionItems, project);
        }
    }
}