using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Response = SecurePipelineScan.VstsService.Response;
using Task = System.Threading.Tasks.Task;

namespace Functions.GlobalPermissionsScan
{
    public class GlobalPermissionsScanProjectOrchestration
    
    {
        [FunctionName(nameof(GlobalPermissionsScanProjectOrchestration))]
        public async Task Run(
            [OrchestrationTrigger] DurableOrchestrationContextBase context,
            ILogger log
        )
        {   
            var project = context.GetInput<Response.Project>();
            
            await context.CallActivityAsync(nameof(GlobalPermissionsScanProjectActivity), project);
        }
    }
}