using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Functions.Activities;
using Functions.Model;
using Microsoft.Azure.WebJobs;

namespace Functions.Orchestrators
{
    public class SingleCompletenessOrchestrator
    {
        [FunctionName(nameof(SingleCompletenessOrchestrator))]
        public async Task RunAsync([OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var (supervisor, allProjectScanners) = context.GetInput<(Orchestrator, IList<Orchestrator>)>();

            var filteredProjectScanners =
                await context.CallActivityAsync<IList<Orchestrator>>(nameof(FilterProjectScannersActivity),
                    (supervisor, allProjectScanners));

            var completenessReport = 
                await context.CallActivityAsync<CompletenessLogItem>(nameof(CreateCompletenessLogItemActivity), 
                    (context.CurrentUtcDateTime, supervisor, filteredProjectScanners));

            await context.CallActivityAsync(nameof(UploadCompletenessLogsActivity), completenessReport);
            
            await Task.WhenAll(filteredProjectScanners
                .Where(f => f.RuntimeStatus == OrchestrationRuntimeStatus.Completed)
                .Select(f => context.CallActivityAsync(nameof(PurgeSingleOrchestratorActivity), f.InstanceId)));
        }
    }
}