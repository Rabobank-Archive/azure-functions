using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;
using AzDoCompliancy.CustomStatus;
using CompletenessCheckFunction.Activities;
using CompletenessCheckFunction.Requests;
using DurableFunctionsAdministration.Client.Response;

namespace CompletenessCheckFunction.Orchestrators
{
    public class SingleAnalysisOrchestrator
    {
        [FunctionName(nameof(SingleAnalysisOrchestrator))]
        public async Task Run([OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            var singleAnalysisRequest = context.GetInput<SingleAnalysisOrchestratorRequest>();
            var totalProjectCount =
                (singleAnalysisRequest.InstanceToAnalyze.CustomStatus as SupervisorOrchestrationStatus)
                ?.TotalProjectCount;
            if (totalProjectCount == null)
                return;

            var allProjectScanOrchestrators =
                await context.CallActivityAsync<List<OrchestrationInstance>>(
                    nameof(GetCompletedOrchestratorsWithNameActivity),
                    "ProjectScanOrchestration");

            var projectScanOrchestratorsForThisAnalysis =
                await context.CallActivityAsync<List<OrchestrationInstance>>(
                    nameof(FilterOrchestratorsForParentIdActivity),
                    new FilterOrchestratorsForParentIdActivityRequest
                    {
                        ParentId = singleAnalysisRequest.InstanceToAnalyze.InstanceId,
                        InstancesToFilter = allProjectScanOrchestrators
                    });

            await context.CallActivityAsync(nameof(UploadAnalysisResultToLogAnalyticsActivity),
                new UploadAnalysisResultToLogAnalyticsActivityRequest
                {
                    TotalProjectCount = (int)totalProjectCount,
                    ScannedProjectCount = projectScanOrchestratorsForThisAnalysis.Count
                });
        }
    }
}