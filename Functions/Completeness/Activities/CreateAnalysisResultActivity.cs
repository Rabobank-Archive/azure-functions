using System.Linq;
using Functions.Completeness.Requests;
using Functions.Completeness.Model;
using Microsoft.Azure.WebJobs;
using System;

namespace Functions.Completeness.Activities
{
    public class CreateAnalysisResultActivity
    {
        [FunctionName(nameof(CreateAnalysisResultActivity))]
        public CompletenessAnalysisResult Run([ActivityTrigger] CreateAnalysisResultActivityRequest request)
        {
            return new CompletenessAnalysisResult
            {
                AnalysisCompleted = request.AnalysisCompleted,
                SupervisorOrchestratorId = request.SupervisorOrchestrator.InstanceId,
                SupervisorStarted = request.SupervisorOrchestrator.CreatedTime,
                TotalProjectCount = request.TotalProjectCount,
                ScannedProjectCount = request.ProjectScanOrchestrators
                    .Where(x => x.RuntimeStatus == OrchestrationRuntimeStatus.Completed)
                    .ToList()
                    .Count,
                FailedProjectIds = String.Join(", ", request.ProjectScanOrchestrators
                    .Where(x => x.RuntimeStatus != OrchestrationRuntimeStatus.Completed)
                    .Select(x => x.InstanceId)
                    .ToList())
            };
        }
    }
}