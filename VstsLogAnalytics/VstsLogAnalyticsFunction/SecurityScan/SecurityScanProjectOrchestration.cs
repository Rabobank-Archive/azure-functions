using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SecurePipelineScan.Rules.Reports;
using Response = SecurePipelineScan.VstsService.Response;

namespace VstsLogAnalyticsFunction
{
    public class SecurityScanProjectOrchestration
    
    {
        [FunctionName(nameof(SecurityScanProjectOrchestration))]
        public async Task<List<SecurityReport>> Run(
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
                        nameof(SecurityScanProjectActivity),
                        project)
                );
            }

            await Task.WhenAll(tasks);

            return tasks.SelectMany(task => task.Result).ToList();
        }
    }
}