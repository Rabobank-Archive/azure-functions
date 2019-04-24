using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SecurePipelineScan.Rules.Reports;
using VstsLogAnalyticsFunction.RepositoryScan;
using Response = SecurePipelineScan.VstsService.Response;

 namespace VstsLogAnalyticsFunction
{
    public class RepositoryScanProjectOrchestration
    {
        [FunctionName(nameof(RepositoryScanProjectOrchestration))]
        public async Task<IEnumerable<RepositoryReport>> Run(
            [OrchestrationTrigger] DurableOrchestrationContextBase context,
            ILogger log
        )

        {
            var projects = context.GetInput<Response.Multiple<Response.Project>>();

            log.LogInformation($"Creating tasks for every project total amount of projects {projects.Count()}");

            var tasks = new List<Task<IEnumerable<RepositoryReport>>>();
            var tasksRepository = new List<Task>();

            foreach (var project in projects)
            {
                log.LogInformation($"Call ActivityReport for project {project.Name}");

                tasks.Add(
                    context.CallActivityAsync<IEnumerable<RepositoryReport>>(
                        nameof(RepositoryScanProjectActivity),
                        project)
                );
                
                log.LogInformation($"Create repository Report for {project.Name}");

                tasksRepository.Add(
                    context.CallActivityAsync(
                        nameof(RepositoryScanPermissionsActivity),
                        project)
                );
            }

            await Task.WhenAll(tasks);
            await Task.WhenAll(tasksRepository);

            return tasks.SelectMany(task => task.Result).ToList();
        }
    }
}