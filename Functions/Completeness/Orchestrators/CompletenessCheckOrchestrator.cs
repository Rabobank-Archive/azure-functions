using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Functions.Completeness.Activities;
using Functions.Completeness.Requests;
using Functions.Completeness.Model;
using Microsoft.Azure.WebJobs;

namespace Functions.Completeness.Orchestrators
{
    public class CompletenessCheckOrchestrator
    {
        [FunctionName(nameof(CompletenessCheckOrchestrator))]
        public async Task RunAsync([OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            (var allSupervisors, var allProjectScanners) =
                await context.CallActivityAsync<(IList<Orchestrator>, IList<Orchestrator>)>(
                    nameof(GetAllOrchestratorsActivity), null);

            var scannedSupervisors =
                await context.CallActivityAsync<IList<string>>(nameof(GetScannedSupervisorsActivity), null);

            var filteredSupervisors =
                await context.CallActivityAsync<IList<Orchestrator>>(nameof(FilterSupervisorsActivity),
                    new FilterSupervisorsRequest
                    {
                        AllSupervisors = allSupervisors,
                        ScannedSupervisors = scannedSupervisors
                    });

            await Task.WhenAll(filteredSupervisors.Select(f =>
                context.CallSubOrchestratorAsync(nameof(SingleCompletenessCheckOrchestrator),
                    new SingleCompletenessCheckRequest
                    {
                        Supervisor = f,
                        AllProjectScanners = allProjectScanners
                    })));

            await Task.WhenAll(allSupervisors.Select(f =>
                context.CallActivityAsync(nameof(PurgeSingleOrchestratorActivity), f.InstanceId)));
        }
    }
}