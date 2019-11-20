using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Functions.Activities;
using Functions.Helpers;
using Functions.Model;
using Microsoft.Azure.WebJobs;
using Response = SecurePipelineScan.VstsService.Response;

namespace Functions.Orchestrators
{
    public class ImpactAnalysisOrchestrator
    {
        [FunctionName(nameof(ImpactAnalysisOrchestrator))]
        public async Task RunAsync(
            [OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var projects = await context.CallActivityWithRetryAsync<IList<Response.Project>>(
                nameof(GetProjectsActivity), RetryHelper.ActivityRetryOptions, null);

            await Task.WhenAll(projects.Select(async p => await EvaluateProjectAsync(context, p)));
        }

        private static async Task EvaluateProjectAsync(DurableOrchestrationContextBase context,
            Response.Project project)
        {
            var productionItems = await context.CallActivityWithRetryAsync<IList<ProductionItem>>(
                nameof(GetDeploymentMethodsActivity), RetryHelper.ActivityRetryOptions, project.Id);

            await Task.WhenAll(productionItems.Select(async p =>
                await EvaluateProductionItemAsync(context, project, p)));
        }

        private static async Task EvaluateProductionItemAsync(DurableOrchestrationContextBase context,
            Response.Project project, ProductionItem productionItem)
        {
            var releases = await context.CallActivityWithRetryAsync<IList<Response.Release>>(
                nameof(GetReleasesActivity), RetryHelper.ActivityRetryOptions,
                (project.Id, productionItem));

            await Task.WhenAll(releases.Select(async r =>
                await EvaluateReleaseAsync(context, project, r, productionItem)));
        }

        private static async Task EvaluateReleaseAsync(DurableOrchestrationContextBase context,
            Response.Project project, Response.Release release, ProductionItem productionItem)
        {
            var approved = await context.CallActivityAsync<bool>(nameof(ScanReleaseActivity), release);

            await context.CallActivityWithRetryAsync(nameof(UploadReleaseLogActivity),
                RetryHelper.ActivityRetryOptions, (project, release, productionItem, approved));
        }
    }
}