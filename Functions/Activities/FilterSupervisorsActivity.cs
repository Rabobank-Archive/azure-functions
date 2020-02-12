using System;
using System.Collections.Generic;
using System.Linq;
using Functions.Model;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;

namespace Functions.Activities
{
    public class FilterSupervisorsActivity
    {
        [FunctionName(nameof(FilterSupervisorsActivity))]
        public IList<Orchestrator> Run([ActivityTrigger] (
            IList<Orchestrator> allSupervisors, IList<string> scannedSupervisors) input, ILogger logger)
        {
            if (input.Item1 == null || input.Item2 == null)
                throw new ArgumentNullException(nameof(input));

            var runtimeStatusesToScan = new List<OrchestrationRuntimeStatus>
            {
                OrchestrationRuntimeStatus.Completed,
                OrchestrationRuntimeStatus.Failed,
                OrchestrationRuntimeStatus.Canceled,
                OrchestrationRuntimeStatus.Terminated
            };

            var result = input.allSupervisors
                .Where(i => !input.scannedSupervisors.Contains(i.InstanceId) &&
                            runtimeStatusesToScan.Contains(i.RuntimeStatus) &&
                            i.CustomStatus != null).ToList();

            logger.LogInformation($"Completed {nameof(FilterSupervisorsActivity)}, filteredSupervisors:{result.Count}");
            return result;
        }
    }
}