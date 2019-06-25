using System.Collections.Generic;
using Functions.Model;
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
            context.SetCustomStatus(new ScanOrchestratorStatus() { Project = project.Name });

            var data = await context.CallActivityAsync<GlobalPermissionsExtensionData>
                (nameof(GlobalPermissionsScanProjectActivity), project);

            await context.CallActivityAsync(nameof(ExtensionDataUploadActivity),
                new ExtensionDataUploadActivityRequest {Data = data, Scope = "globalpermissions"});
        }
    }
}