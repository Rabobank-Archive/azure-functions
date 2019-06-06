using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Response = SecurePipelineScan.VstsService.Response;
using Task = System.Threading.Tasks.Task;

namespace Functions.ItemScan
{
    public class ItemScanProjectOrchestration
    {
        [FunctionName(nameof(ItemScanProjectOrchestration))]
        public async Task Run(
            [OrchestrationTrigger] DurableOrchestrationContextBase context,
            ILogger log)
        {
            var projects = context.GetInput<IList<Response.Project>>();
            log.LogInformation($"Creating tasks for every project total amount of projects {projects.Count}");

            foreach (var project in projects)
            {
                log.LogInformation($"Call ActivityNameRepos for project {project.Name}");
                await context.CallActivityAsync(ItemScanPermissionsActivity.ActivityNameRepos, project);
                log.LogInformation($"Call ActivityNameBuilds for project {project.Name}");
                await context.CallActivityAsync(ItemScanPermissionsActivity.ActivityNameBuilds, project);
                log.LogInformation($"Call ActivityNameReleases for project {project.Name}");
                await context.CallActivityAsync(ItemScanPermissionsActivity.ActivityNameReleases, project);
            }
        }
    }
}