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
    public class SingleAnalysisOrchestrator
    {
        [FunctionName(nameof(SingleAnalysisOrchestrator))]
        public Task RunAsync([OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            return RunInternalAsync(context);
        }

        private async Task RunInternalAsync(DurableOrchestrationContextBase context)
        {
            var singleAnalysisRequest = context.GetInput<SingleAnalysisOrchestratorRequest>();

            var totalProjectCount = await context.CallActivityAsync<int?>(
                nameof(GetTotalProjectCountFromSupervisorOrchestrationStatusActivity),
                singleAnalysisRequest.InstanceToAnalyze);
            
            if (totalProjectCount == null)
                return;

            var projectScanOrchestratorsForThisAnalysis = 
                await context.CallActivityAsync<IList<SimpleDurableOrchestrationStatus>>(
                    nameof(FilterOrchestratorsForParentIdActivity),
                    new FilterOrchestratorsForParentIdActivityRequest
                    {
                        ParentId = singleAnalysisRequest.InstanceToAnalyze.InstanceId,
                        InstancesToFilter = singleAnalysisRequest.AllProjectScanOrchestrators
                    });

            var analysisResult = await context.CallActivityAsync<CompletenessAnalysisResult>(
                nameof(CreateAnalysisResultActivity), 
                new CreateAnalysisResultActivityRequest
                {
                    AnalysisCompleted = context.CurrentUtcDateTime,
                    SupervisorOrchestrator = singleAnalysisRequest.InstanceToAnalyze,
                    TotalProjectCount = (int)totalProjectCount,
                    ProjectScanOrchestrators = projectScanOrchestratorsForThisAnalysis
                });

            await context.CallActivityAsync(nameof(UploadAnalysisResultToLogAnalyticsActivity), analysisResult);
            
            await Task.WhenAll(projectScanOrchestratorsForThisAnalysis.Select(f =>
                context.CallActivityAsync(nameof(PurgeSingleOrchestratorActivity), f.InstanceId)));
        }
    }
}