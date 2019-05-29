﻿using System.Collections.Generic;
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

            var tasks = new List<Task>();
            foreach (var project in projects)
            {
                log.LogInformation($"Call ActivityReport for project {project.Name}");
                tasks.Add(context.CallActivityAsync(ItemScanPermissionsActivity.ActivityName, project));
            }

            await Task.WhenAll(tasks);
        }
    }
}