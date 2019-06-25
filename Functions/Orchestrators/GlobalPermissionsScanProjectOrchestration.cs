using System.Collections.Generic;
using Functions.Activities;
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
            [OrchestrationTrigger] DurableOrchestrationContextBase context)
        {   
            var project = context.GetInput<Response.Project>();
            context.SetCustomStatus(new ScanOrchestratorStatus() { Project = project.Name });

            var data = await context.CallActivityAsync<GlobalPermissionsExtensionData>
                (nameof(GlobalPermissionsScanProjectActivity), project);

            await Task.WhenAll(
                context.CallActivityAsync(nameof(ExtensionDataUploadActivity),
                    new ExtensionDataUploadActivityRequest {Data = data, Scope = "globalpermissions"}),

                context.CallActivityAsync(nameof(LogAnalyticsUploadActivity),
                    new LogAnalyticsUploadActivityRequest {PreventiveLogItems = data.Flatten()})
            );

        }
    }
}