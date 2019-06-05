using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Functions.RepositoryScan;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Response = SecurePipelineScan.VstsService.Response;

 namespace Functions
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

            var tasksRepos = new List<Task>();
            var tasksBuilds = new List<Task>();
            var tasksReleases = new List<Task>();
            foreach (var project in projects)
            {
                log.LogInformation($"Call ActivityReport for project {project.Name}");
                tasksRepos.Add(context.CallActivityAsync(ItemScanPermissionsActivity.ActivityNameRepos, project));
            }

            await Task.WhenAll(tasksRepos);
            
            foreach (var project in projects)
            {
                log.LogInformation($"Call ActivityReport for project {project.Name}");
                tasksBuilds.Add(context.CallActivityAsync(ItemScanPermissionsActivity.ActivityNameBuilds, project));
            }

            await Task.WhenAll(tasksBuilds);
            
            foreach (var project in projects)
            {
                log.LogInformation($"Call ActivityReport for project {project.Name}");
                tasksReleases.Add(context.CallActivityAsync(ItemScanPermissionsActivity.ActivityNameReleases, project));
            }

            await Task.WhenAll(tasksReleases);
        }
    }
}