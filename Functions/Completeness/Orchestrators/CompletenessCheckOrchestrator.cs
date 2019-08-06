using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Functions.Completeness.Activities;
using Functions.Completeness.Requests;
using Microsoft.Azure.WebJobs;

namespace Functions.Completeness.Orchestrators
{
    public class CompletenessCheckOrchestrator
    {
        [FunctionName(nameof(CompletenessCheckOrchestrator))]
        public async Task RunAsync([OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            var scansToVerify = await context.CallActivityAsync<List<DurableOrchestrationStatus>>(
                nameof(GetCompletedOrchestratorsWithNameActivity), "ProjectScanSupervisor");

            var alreadyVerifiedScans = await context.CallActivityAsync<List<string>>(
                nameof(GetCompletedScansFromLogAnalyticsActivity), null);

            var filteredScansToVerify = await context.CallActivityAsync<List<DurableOrchestrationStatus>>(
                nameof(FilterAlreadyAnalyzedOrchestratorsActivity),
                new FilterAlreadyAnalyzedOrchestratorsActivityRequest
                { InstancesToAnalyze = scansToVerify, InstanceIdsAlreadyAnalyzed = alreadyVerifiedScans });

            if (filteredScansToVerify.Count > 0)
            {
                var allProjectScanOrchestrators =
                    await context.CallActivityAsync<List<DurableOrchestrationStatus>>(
                        nameof(GetCompletedOrchestratorsWithNameActivity), "ProjectScanOrchestration");

                await Task.WhenAll(filteredScansToVerify.Select(f =>
                    context.CallSubOrchestratorAsync(nameof(SingleAnalysisOrchestrator),
                        new SingleAnalysisOrchestratorRequest
                        {
                            InstanceToAnalyze = f,
                            AllProjectScanOrchestrators = allProjectScanOrchestrators
                        })));
            }
        }
    }
}


