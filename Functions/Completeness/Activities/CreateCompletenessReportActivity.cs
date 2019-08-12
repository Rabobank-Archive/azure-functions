using System.Linq;
using Functions.Completeness.Requests;
using Functions.Completeness.Model;
using Microsoft.Azure.WebJobs;
using System;
using Newtonsoft.Json;
using AzDoCompliancy.CustomStatus.Converter;
using AzDoCompliancy.CustomStatus;

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
                    .Where(x => x.RuntimeStatus == OrchestrationRuntimeStatus.Completed)
                    .ToList()
                    .Count,
                FailedProjectIds = String.Join(", ", request.ProjectScanners
                    .Where(x => x.RuntimeStatus != OrchestrationRuntimeStatus.Completed)
                    .Select(x => GetProjectId(x.InstanceId))
                    .ToList())
            };
        }

        private static string GetProjectId(string instanceId)
        {
            var idParts = instanceId.Split(':');
            return idParts.Length == 2 ? idParts.Last() : string.Empty;
        }
    }
}