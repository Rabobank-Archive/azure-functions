using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzDoCompliancy.CustomStatus;
using Functions.Activities;
using Functions.Helpers;
using Functions.Model;
using Functions.Starters;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService.Response;
using Task = System.Threading.Tasks.Task;

namespace Functions.Orchestrators
{
    public class BuildPipelinesOrchestrator
    {
        private readonly EnvironmentConfig _config;

        public BuildPipelinesOrchestrator(EnvironmentConfig config) => _config = config;

        [FunctionName(nameof(BuildPipelinesOrchestrator))]
        public async Task<IList<ProductionItem>> RunAsync(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var (project, productionItems) = context.GetInput<(Project, List<ProductionItem>)>();

            context.SetCustomStatus(new ScanOrchestrationStatus
            {
                Project = project.Name,
                Scope = RuleScopes.BuildPipelines
            });

            var buildPipelines = await context.CallActivityWithRetryAsync<List<BuildDefinition>>(
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
                    StartScanActivityAsync(context, b, project, productionItems)))
            };

            await context.CallActivityAsync(nameof(UploadPreventiveRuleLogsActivity),
                data.Flatten(RuleScopes.BuildPipelines, context.InstanceId));

            await context.CallActivityAsync(nameof(UploadExtensionDataActivity),
                (buildPipelines: data, RuleScopes.BuildPipelines));

            return LinkConfigurationItemHelper.LinkCisToRepositories(buildPipelines,
                productionItems, project);
        }

        private static Task<ItemExtensionData> StartScanActivityAsync(IDurableOrchestrationContext context, 
                BuildDefinition b, Project project, IEnumerable<ProductionItem> productionItems) => 
            context.CallActivityWithRetryAsync<ItemExtensionData>(nameof(ScanBuildPipelinesActivity),
                RetryHelper.ActivityRetryOptions, (project, b, LinkConfigurationItemHelper.GetCiIdentifiers(
                productionItems
                    .Where(r => r.ItemId == b.Id))));
    }
}