using System.Collections.Generic;
using System.Linq;
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
        public async Task RunAsync([OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            var request = context.GetInput<ItemOrchestratorRequest>();

            context.SetCustomStatus(new ScanOrchestrationStatus
            {
                Project = request.Project.Name,
                Scope = RuleScopes.BuildPipelines
            });

            var buildDefinitions = await context.CallActivityWithRetryAsync<List<BuildDefinition>>(
                nameof(BuildDefinitionsActivity), RetryHelper.ActivityRetryOptions, request.Project);

            var data = new ItemsExtensionData
            {
                Id = request.Project.Name,
                Date = context.CurrentUtcDateTime,
                RescanUrl = ProjectScanHttpStarter.RescanUrl(_config, request.Project.Name, RuleScopes.BuildPipelines),
                HasReconcilePermissionUrl = ReconcileFunction.HasReconcilePermissionUrl(_config, request.Project.Id),
                Reports = await Task.WhenAll(buildDefinitions.Select(b =>
                    context.CallActivityWithRetryAsync<ItemExtensionData>(nameof(BuildPipelinesScanActivity),
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

            await context.CallActivityAsync(nameof(LogAnalyticsUploadActivity),
                new LogAnalyticsUploadActivityRequest
                {
                    PreventiveLogItems = data.Flatten(RuleScopes.BuildPipelines, context.InstanceId)
                });

            await context.CallActivityAsync(nameof(ExtensionDataUploadActivity),
                (buildPipelines: data, RuleScopes.BuildPipelines));
        }
    }
}