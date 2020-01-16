using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Functions.Activities;
using Functions.Model;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Functions.Orchestrators
{
    public class CompletenessOrchestrator
    {
        [FunctionName(nameof(CompletenessOrchestrator))]
        public async Task RunAsync([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var (allSupervisors, allProjectScanners) =
                await context.CallActivityAsync<(IList<Orchestrator>, IList<Orchestrator>)>(
                    nameof(GetOrchestratorsToScanActivity), null);

            var scannedSupervisorIds =
                await context.CallActivityAsync<IList<string>>(nameof(GetScannedSupervisorsActivity), null);

            var filteredSupervisors =
                await context.CallActivityAsync<IList<Orchestrator>>(nameof(FilterSupervisorsActivity),
                    (allSupervisors, scannedSupervisorIds));

            await Task.WhenAll(filteredSupervisors.Select(f =>
                context.CallSubOrchestratorAsync(nameof(SingleCompletenessOrchestrator),
                    (f, allProjectScanners))));

            await Task.WhenAll(filteredSupervisors.Select(f =>
                context.CallActivityAsync(nameof(PurgeSingleOrchestratorActivity), f.InstanceId)));
        }
    }
}