using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
            var numberOfProjects = projects.Count;
            
            log.LogInformation($"Creating tasks for every project total amount of projects {numberOfProjects}");
            
            var tasks = new List<Task<IEnumerable<SecurityReport>>>();
            int currentProject = 0;
            int parallelBatchIndex = 0;
            int batch = 1;
            int maxParallel = 20;

            while (currentProject < numberOfProjects)
            {
                while (currentProject < numberOfProjects && parallelBatchIndex < maxParallel)
                {
                    parallelBatchIndex++;
                    var project = projects[currentProject];
                    log.LogInformation($"Create securityReport for {project.Name}");
                    log.LogInformation($"Project nr {currentProject}, batch number {batch}, project {parallelBatchIndex} of {maxParallel}");
                
                    tasks.Add(
                        context.CallActivityAsync<IEnumerable<SecurityReport>>(
                            nameof(SecurityScanProjectActivity),
                            project)
                    );
                    currentProject++;
                }
                parallelBatchIndex = 0;
                batch++;
                await Task.WhenAll(tasks);
            }
            return tasks.SelectMany(task => task.Result).ToList();
        }
    }
}