using AzDoCompliancy.CustomStatus;
using Functions.Activities;
using Functions.Model;
using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;
using Functions.Helpers;
using Functions.Starters;
using System.Collections.Generic;
using SecurePipelineScan.Rules.Security;

namespace Functions.Orchestrators
{
    public class GlobalPermissionsOrchestration
    {
        private readonly EnvironmentConfig _config;

        public GlobalPermissionsOrchestration(EnvironmentConfig config)
        {
            _config = config;
        }

        [FunctionName(nameof(GlobalPermissionsOrchestration))]
        public async Task RunAsync([OrchestrationTrigger]DurableOrchestrationContextBase context)
        {
            var request = context.GetInput<ItemOrchestratorRequest>();

            context.SetCustomStatus(new ScanOrchestrationStatus
            {
                Project = request.Project.Name, Scope = RuleScopes.GlobalPermissions
            });

            var data = new ItemsExtensionData
            {
                Id = request.Project.Name,
                Date = context.CurrentUtcDateTime,
                RescanUrl = ProjectScanHttpStarter.RescanUrl(
                    _config, request.Project.Name, RuleScopes.GlobalPermissions),
                HasReconcilePermissionUrl = ReconcileFunction.HasReconcilePermissionUrl(
                    _config, request.Project.Id),
                Reports = new List<ItemExtensionData>
                {
                    await context.CallActivityWithRetryAsync<ItemExtensionData>(nameof(
                        GlobalPermissionsScanActivity), RetryHelper.ActivityRetryOptions, request)
                }
            };

            await context.CallActivityAsync(nameof(LogAnalyticsUploadActivity), new LogAnalyticsUploadActivityRequest
            {
                PreventiveLogItems = data.Flatten(RuleScopes.GlobalPermissions, context.InstanceId)
            });

            await context.CallActivityAsync(nameof(ExtensionDataUploadActivity), 
                (permissions: data, RuleScopes.GlobalPermissions));
        }
    }
}