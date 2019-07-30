using System.Collections.Generic;
using System.Linq;
using CompletenessCheckFunction.Activities;
using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;
using CompletenessCheckFunction.Requests;
using DurableFunctionsAdministration.Client.Response;

namespace CompletenessCheckFunction.Orchestrators
{
    public class CompletenessCheckOrchestrator
    {
        [FunctionName(nameof(CompletenessCheckOrchestrator))]
        public async Task RunAsync([OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            var scansToVerify = await context.CallActivityAsync<List<OrchestrationInstance>>(
                nameof(GetCompletedOrchestratorsWithNameActivity), "ProjectScanSupervisor")
                .ConfigureAwait(false);

            var alreadyVerifiedScans = await context.CallActivityAsync<List<string>>(
                nameof(GetCompletedScansFromLogAnalyticsActivity), null)
                .ConfigureAwait(false);

            var filteredScansToVerify = await context.CallActivityAsync<List<OrchestrationInstance>>(
                nameof(FilterAlreadyAnalyzedOrchestratorsActivity),
                new FilterAlreadyAnalyzedOrchestratorsActivityRequest
                { InstancesToAnalyze = scansToVerify, InstanceIdsAlreadyAnalyzed = alreadyVerifiedScans })
                .ConfigureAwait(false);

            var allProjectScanOrchestrators =
                await context.CallActivityAsync<List<OrchestrationInstance>>(
                    nameof(GetCompletedOrchestratorsWithNameActivity), "ProjectScanOrchestration")
                .ConfigureAwait(false);

            await Task.WhenAll(filteredScansToVerify.Select(f =>
                context.CallSubOrchestratorAsync(nameof(SingleAnalysisOrchestrator),
                    new SingleAnalysisOrchestratorRequest
                    {
                        InstanceToAnalyze = f,
                        AllProjectScanOrchestrators = allProjectScanOrchestrators
                    })))
                .ConfigureAwait(false);
        }
    }
}


