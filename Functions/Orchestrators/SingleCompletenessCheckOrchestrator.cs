using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Functions.Completeness.Activities;
using Functions.Completeness.Model;
using Functions.Completeness.Requests;
using Microsoft.Azure.WebJobs;

namespace Functions.Completeness.Orchestrators
{
    public class SingleCompletenessCheckOrchestrator
    {
        [FunctionName(nameof(SingleCompletenessCheckOrchestrator))]
        public Task RunAsync([OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            return RunInternalAsync(context);
        }

        private async Task RunInternalAsync(DurableOrchestrationContextBase context)
        {
            var request = context.GetInput<SingleCompletenessCheckRequest>();

            var filteredProjectScanners =
                await context.CallActivityAsync<IList<Orchestrator>>(nameof(FilterProjectScannersActivity), request);

            var completenessReport = 
                await context.CallActivityAsync<CompletenessReport>(nameof(CreateCompletenessReportActivity), 
                    new CreateCompletenessReportRequest
                    {
                        AnalysisCompleted = context.CurrentUtcDateTime,
                        Supervisor = request.Supervisor,
                        ProjectScanners = filteredProjectScanners
                    });

            await context.CallActivityAsync(nameof(UploadCompletenessLogsActivity), completenessReport);
            
            await Task.WhenAll(filteredProjectScanners
                .Where(f => f.RuntimeStatus == OrchestrationRuntimeStatus.Completed)
                .Select(f => context.CallActivityAsync(nameof(PurgeSingleOrchestratorActivity), f.InstanceId)));
        }
    }
}