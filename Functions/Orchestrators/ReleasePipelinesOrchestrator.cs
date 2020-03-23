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
    public class ReleasePipelinesOrchestrator
    {
        private readonly EnvironmentConfig _config;

        public ReleasePipelinesOrchestrator(EnvironmentConfig config) => _config = config;

        [FunctionName(nameof(ReleasePipelinesOrchestrator))]
        public async Task RunAsync(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var (project, scanDate) = 
                context.GetInput<(Response.Project, DateTime)>();
           
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
                    StartScanActivityAsync(context, r, project)))
            };

            await context.CallActivityAsync(nameof(UploadExtensionDataActivity),
                (releasePipelines: data, RuleScopes.ReleasePipelines));
        }

        private static Task<ItemExtensionData> StartScanActivityAsync(IDurableOrchestrationContext context,
                Response.ReleaseDefinition r, Response.Project project) =>
            context.CallActivityWithRetryAsync<ItemExtensionData>(
                nameof(ScanReleasePipelinesActivity), RetryHelper.ActivityRetryOptions,
                (project, r));
    }
}