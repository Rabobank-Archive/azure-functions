using System.Linq;
using Functions.Model;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using AzDoCompliancy.CustomStatus.Converter;
using AzDoCompliancy.CustomStatus;
using Functions.Helpers;
using System;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Functions.Activities
{
    public class CreateCompletenessLogItemActivity
    {
        [FunctionName(nameof(CreateCompletenessLogItemActivity))]
        public CompletenessLogItem Run([ActivityTrigger] (DateTime, Orchestrator, IList<Orchestrator>) input)
        {
            if (input.Item2 == null || input.Item3 == null)
                throw new ArgumentNullException(nameof(input));

            var analysisCompleted = input.Item1;
            var supervisor = input.Item2;
            var projectScanners = input.Item3;

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
                    .Select(x => OrchestrationHelper.GetProjectIdForProjectOrchestrator(x.InstanceId)))
            };
        }
    }
}