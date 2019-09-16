using System.Collections.Generic;
using System.Linq;
using AzDoCompliancy.CustomStatus;
using Functions.Activities;
using Functions.Helpers;
using Functions.Model;
using Functions.Starters;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.VstsService.Response;
using Task = System.Threading.Tasks.Task;

namespace Functions.Orchestrators
{
    public class RepositoriesOrchestration
    {
        private readonly EnvironmentConfig _config;

        public RepositoriesOrchestration(EnvironmentConfig config)
        {
            _config = config;
        }
        
        [FunctionName(nameof(RepositoriesOrchestration))]
        public async Task RunAsync([OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            var request = context.GetInput<ItemOrchestratorRequest>();

            context.SetCustomStatus(new ScanOrchestrationStatus
            {
                Project = request.Project.Name,
                Scope = RuleScopes.Repositories
            });

            var repositories = await context.CallActivityWithRetryAsync<List<Repository>>(
                nameof(RepositoriesForProjectActivity), RetryHelper.ActivityRetryOptions, request.Project);

            var data = new ItemsExtensionData
            {
                Id = request.Project.Name,
                Date = context.CurrentUtcDateTime,
                RescanUrl = ProjectScanHttpStarter.RescanUrl(_config, request.Project.Name, RuleScopes.Repositories),
                HasReconcilePermissionUrl = ReconcileFunction.HasReconcilePermissionUrl(_config, request.Project.Id),
                Reports = await Task.WhenAll(repositories.Select(r =>
                    context.CallActivityWithRetryAsync<ItemExtensionData>(nameof(RepositoriesScanActivity),
                        RetryHelper.ActivityRetryOptions, new RepositoriesScanActivityRequest
                        {
                            Project = request.Project,
                            Repository = r,
                            CiIdentifiers = request.ProductionItems
                                .Where(p => p.ItemId == r.Id)
                                .SelectMany(p => p.CiIdentifiers)
                                .ToList()
                        })))
            };
            
            await context.CallActivityAsync(nameof(LogAnalyticsUploadActivity),
                new LogAnalyticsUploadActivityRequest
                {
                    PreventiveLogItems = data.Flatten(RuleScopes.Repositories, context.InstanceId)
                });

            await context.CallActivityAsync(nameof(ExtensionDataUploadActivity),
                (repositories: data, RuleScopes.Repositories));
        }
    }
}