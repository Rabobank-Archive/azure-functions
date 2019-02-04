using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Rules.Reports;
using SecurePipelineScan.VstsService;
using Response = SecurePipelineScan.VstsService.Response;
using VstsLogAnalytics.Common;
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
            
            var newList50Projects = (from project in projects
                orderby project.Name select project).Take(50);

            log.LogInformation($"Creating tasks for every project total amount of projects {projects.Count()}");
            
            var tasks = new List<Task<IEnumerable<SecurityReport>>>();
            foreach (var project in newList50Projects)
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
            
            
//            var tasks = projects.Select(x => context.CallActivityAsync<int>(nameof(CreateSecurityReport), x));
//
//            var enumerable = tasks.ToList();
//            await Task.WhenAll(enumerable);
//
//            return enumerable.Sum(t => t.Result).ToString();
        }
    }
}