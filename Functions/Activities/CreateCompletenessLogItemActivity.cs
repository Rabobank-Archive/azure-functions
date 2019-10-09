using System.Linq;
using Functions.Model;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using AzDoCompliancy.CustomStatus.Converter;
using AzDoCompliancy.CustomStatus;
using Functions.Helpers;
using System;
using System.Collections.Generic;

namespace Functions.Activities
{
    public class CreateCompletenessLogItemActivity
    {
        [FunctionName(nameof(CreateCompletenessLogItemActivity))]
        public CompletenessLogItem Run([ActivityTrigger] (DateTime, Orchestrator, IList<Orchestrator>) data)
        {
            if (data.Item2 == null || data.Item3 == null)
                throw new ArgumentNullException(nameof(data));

            var analysisCompleted = data.Item1;
            var supervisor = data.Item2;
            var projectScanners = data.Item3;

            var serializer = new JsonSerializer();
            serializer.Converters.Add(new CustomStatusConverter());

            return new CompletenessLogItem
            {
                AnalysisCompleted = analysisCompleted,
                SupervisorId = supervisor.InstanceId,
                SupervisorStarted = supervisor.CreatedTime,
                TotalProjectCount = (supervisor.CustomStatus?
                    .ToObject<CustomStatusBase>(serializer) as SupervisorOrchestrationStatus)?
                        .TotalProjectCount,
                ScannedProjectCount = projectScanners
                    .Count(x => x.RuntimeStatus == OrchestrationRuntimeStatus.Completed),
                FailedProjectIds = string.Join(", ", projectScanners
                    .Where(x => x.RuntimeStatus != OrchestrationRuntimeStatus.Completed)
                    .Select(x => OrchestrationHelper.GetProjectId(x.InstanceId)))
            };
        }
    }
}