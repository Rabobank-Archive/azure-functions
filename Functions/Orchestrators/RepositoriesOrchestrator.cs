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
    public class RepositoriesOrchestrator
    {
        private readonly EnvironmentConfig _config;

        public RepositoriesOrchestrator(EnvironmentConfig config) => _config = config;

        [FunctionName(nameof(RepositoriesOrchestrator))]
        public async Task RunAsync([OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            var (project, productionItems) = context.GetInput<(Project, List<ProductionItem>)>();

            context.SetCustomStatus(new ScanOrchestrationStatus
            {
                Project = project.Name,
                Scope = RuleScopes.Repositories
            });

            var (repositories, policies) = await context.CallActivityWithRetryAsync<(IEnumerable<Repository>, 
                IEnumerable<MinimumNumberOfReviewersPolicy>)>(nameof(GetRepositoriesAndPoliciesActivity), 
                RetryHelper.ActivityRetryOptions, project);

            var data = new ItemsExtensionData
            {
                Id = project.Name,
                Date = context.CurrentUtcDateTime,
                RescanUrl = ProjectScanHttpStarter.RescanUrl(_config, project.Name, 
                    RuleScopes.Repositories),
                HasReconcilePermissionUrl = ReconcileFunction.HasReconcilePermissionUrl(_config, 
                    project.Id),
                Reports = await Task.WhenAll(repositories.Select(r =>
                    context.CallActivityWithRetryAsync<ItemExtensionData>(nameof(ScanRepositoriesActivity),
                    RetryHelper.ActivityRetryOptions, (project, r, policies, productionItems
                        .Where(p => p.ItemId == r.Id)
                        .SelectMany(p => p.DeploymentInfo)
                        .Select(d => d.CiIdentifier)
                        .Distinct()
                        .ToList()))))
            };
            
            await context.CallActivityAsync(nameof(UploadPreventiveRuleLogsActivity),
                data.Flatten(RuleScopes.Repositories, context.InstanceId));

            await context.CallActivityAsync(nameof(UploadExtensionDataActivity),
                (repositories: data, RuleScopes.Repositories));
        }
    }
}