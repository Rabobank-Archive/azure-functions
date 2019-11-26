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

            await Task.WhenAll(projects
                .Select(async p => await EvaluateProjectAsync(context, p)));
        }

        private static async Task EvaluateProjectAsync(DurableOrchestrationContextBase context,
            Response.Project project)
        {
            var productionItems = await context.CallActivityWithRetryAsync<IList<ProductionItem>>(
                nameof(GetDeploymentMethodsActivity), RetryHelper.ActivityRetryOptions, project.Id);

            await Task.WhenAll(productionItems
                .Where(p => p.DeploymentInfo.Any(d => d.IsSoxApplication))
                .Select(async p => await EvaluateReleasePipelineAsync(context, project, p.ItemId,
                    p.DeploymentInfo.Where(d => d.IsSoxApplication))));
        }

        private static async Task EvaluateReleasePipelineAsync(
            DurableOrchestrationContextBase context, Response.Project project, 
            string releasePipelineId, IEnumerable<DeploymentMethod> deploymentMethods)
        {
            var releases = await context.CallActivityWithRetryAsync<IList<Response.Release>>(
                nameof(GetReleasesActivity), RetryHelper.ActivityRetryOptions,
                (project.Id, releasePipelineId, deploymentMethods));

            await Task.WhenAll(releases.Select(async r => 
                await EvaluateReleaseAsync(context, project.Name, r, releasePipelineId, deploymentMethods)));
        }

        private static async Task EvaluateReleaseAsync(
            DurableOrchestrationContextBase context, string projectName, Response.Release release, 
            string releasePipelineId, IEnumerable<DeploymentMethod> deploymentMethods)
        {
            var approved = await context.CallActivityAsync<bool>(nameof(ScanReleaseActivity), release);

            await context.CallActivityWithRetryAsync(nameof(UploadReleaseLogActivity),
                RetryHelper.ActivityRetryOptions, (projectName, release.Id, releasePipelineId, 
                deploymentMethods, approved));
        }
    }
}