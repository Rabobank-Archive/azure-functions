using System.Linq;
using Functions.Completeness.Requests;
using Functions.Completeness.Model;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using AzDoCompliancy.CustomStatus.Converter;
using AzDoCompliancy.CustomStatus;
using Functions.Helpers;

namespace Functions.Completeness.Activities
{
    public class CreateCompletenessReportActivity
    {
        [FunctionName(nameof(CreateCompletenessReportActivity))]
        public CompletenessReport Run([ActivityTrigger] CreateCompletenessReportRequest request)
        {
            var serializer = new JsonSerializer();
            serializer.Converters.Add(new CustomStatusConverter());

            return new CompletenessReport
            {
                AnalysisCompleted = request.AnalysisCompleted,
                SupervisorId = request.Supervisor.InstanceId,
                SupervisorStarted = request.Supervisor.CreatedTime,
                TotalProjectCount = (request.Supervisor.CustomStatus?
                    .ToObject<CustomStatusBase>(serializer) as SupervisorOrchestrationStatus)?.TotalProjectCount,
                ScannedProjectCount = request.ProjectScanners
                    .Count(x => x.RuntimeStatus == OrchestrationRuntimeStatus.Completed),
                FailedProjectIds = string.Join(", ", request.ProjectScanners
                    .Where(x => x.RuntimeStatus != OrchestrationRuntimeStatus.Completed)
                    .Select(x => OrchestrationIdHelper.GetProjectId(x.InstanceId)))
            };
        }
    }
}