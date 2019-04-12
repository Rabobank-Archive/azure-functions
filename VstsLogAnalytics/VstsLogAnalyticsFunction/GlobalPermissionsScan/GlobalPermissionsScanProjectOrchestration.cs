using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Response = SecurePipelineScan.VstsService.Response;
using Task = System.Threading.Tasks.Task;

namespace VstsLogAnalyticsFunction.GlobalPermissionsScan
{
    public class GlobalPermissionsScanProjectOrchestration
    
    {
        [FunctionName(nameof(GlobalPermissionsScanProjectOrchestration))]
        public async Task Run(
            [OrchestrationTrigger] DurableOrchestrationContextBase context,
            ILogger log
        )
        {   
            var projects = context.GetInput<List<Response.Project>>();
            
            log.LogInformation($"Creating tasks for every project total amount of projects {projects.Count}");
            
            var tasks = new List<Task>();
            int currentProject = 1;

            foreach(var project in projects)
            {
                log.LogInformation($"Create Global Permissions Report for {project.Name}");
                log.LogInformation($"Project nr {currentProject} of {projects.Count}");
                
                tasks.Add(
                    context.CallActivityAsync(
                        nameof(GlobalPermissionsScanProjectActivity),
                        project)
                );
                currentProject++;
            }
            await Task.WhenAll(tasks);
        }
    }
}