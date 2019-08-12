using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Functions.Completeness.Activities;
using Functions.Completeness.Requests;
using Functions.Completeness.Responses;
using Microsoft.Azure.WebJobs;

namespace Functions.Completeness.Orchestrators
{
    public class CompletenessCheckOrchestrator
    {
        [FunctionName(nameof(CompletenessCheckOrchestrator))]
        public async Task RunAsync([OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            (var projectScanSupervisors, var projectScanOrchestrators) = 
                await context.CallActivityAsync<(IList<SimpleDurableOrchestrationStatus>, IList<SimpleDurableOrchestrationStatus>)>
                    (nameof(GetAllOrchestratorsActivity), null);

            var alreadyVerifiedScans = await context.CallActivityAsync<IList<string>>(
                nameof(GetCompletedScansFromLogAnalyticsActivity), null);

            var filteredScansToVerify = await context.CallActivityAsync<IList<SimpleDurableOrchestrationStatus>>(
                nameof(FilterAlreadyAnalyzedOrchestratorsActivity),
                new FilterAlreadyAnalyzedOrchestratorsActivityRequest
                { InstancesToAnalyze = projectScanSupervisors, InstanceIdsAlreadyAnalyzed = alreadyVerifiedScans });

            if (filteredScansToVerify.Count > 0)
            {
                await Task.WhenAll(filteredScansToVerify.Select(f =>
                    context.CallSubOrchestratorAsync(nameof(SingleAnalysisOrchestrator),
                        new SingleAnalysisOrchestratorRequest
                        {
                            InstanceToAnalyze = f,
                            AllProjectScanOrchestrators = projectScanOrchestrators
                        })));
            }

            await Task.WhenAll(projectScanSupervisors.Select(f =>
                context.CallActivityAsync(nameof(PurgeSingleOrchestratorActivity), f.InstanceId)));
        }
    }
}