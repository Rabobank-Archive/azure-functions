using Functions.Activities;
using Functions.Model;
using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;
using Functions.Helpers;
using Functions.Starters;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Response = SecurePipelineScan.VstsService.Response;
using System;
using AzureDevOps.Compliance.Rules;

namespace Functions.Orchestrators
{
    public class GlobalPermissionsOrchestrator
    {
        private readonly EnvironmentConfig _config;

        public GlobalPermissionsOrchestrator(EnvironmentConfig config) => _config = config;

        [FunctionName(nameof(GlobalPermissionsOrchestrator))]
        public async Task RunAsync([OrchestrationTrigger]IDurableOrchestrationContext context)
        {
            var (project, scanDate) = 
                context.GetInput<(Response.Project, DateTime)>();

            var data = new ItemsExtensionData
            {
                Id = project.Name,
                Date = context.CurrentUtcDateTime,
                RescanUrl = ProjectScanHttpStarter.RescanUrl(
                    _config, project.Name, RuleScopes.GlobalPermissions),
                HasReconcilePermissionUrl = ReconcileFunction.HasReconcilePermissionUrl(
                    _config, project.Id),
                Reports = new List<ItemExtensionData>
                {
                    await context.CallActivityWithRetryAsync<ItemExtensionData>(nameof(
                        ScanGlobalPermissionsActivity), RetryHelper.ActivityRetryOptions, 
                        project)
                }
            };

            await context.CallActivityAsync(nameof(UploadExtensionDataActivity), 
                (permissions: data, RuleScopes.GlobalPermissions));
        }
    }
}