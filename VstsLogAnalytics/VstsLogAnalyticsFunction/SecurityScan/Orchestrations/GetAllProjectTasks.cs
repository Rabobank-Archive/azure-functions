using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SecurePipelineScan.Rules.Reports;
using Response = SecurePipelineScan.VstsService.Response;
using VstsLogAnalyticsFunction.SecurityScan.Activites;

namespace VstsLogAnalyticsFunction.SecurityScan.Orchestrations
{
    public static class GetAllProjectTasks
    
    {
        [FunctionName(nameof(GetAllProjectTasks))]
        public static async Task<List<SecurityReport>> Run(
            [OrchestrationTrigger] DurableOrchestrationContextBase context,
            ILogger log
        )

        {
            
            var projects = context.GetInput<List<Response.Project>>();
            
            log.LogInformation($"Creating tasks for every project total amount of projects {projects.Count()}");
            
            var tasks = new List<Task<IEnumerable<SecurityReport>>>();
            foreach (var project in projects)
            {
                
                log.LogInformation($"Create securityReport for {project.Name}");
                
                tasks.Add(
                    context.CallActivityAsync<IEnumerable<SecurityReport>>(
                        nameof(CreateSecurityReport),
                        project)
                );
            }

            await Task.WhenAll(tasks);

            return tasks.SelectMany(task => task.Result).ToList();
        }
    }
}