using System;
using System.Collections.Generic;
using System.Linq;
using Functions.Model;
using Microsoft.Azure.WebJobs;

namespace Functions.Activities
{
    public class FilterSupervisorsActivity
    {
        [FunctionName(nameof(FilterSupervisorsActivity))]
        public IList<Orchestrator> Run([ActivityTrigger] (IList<Orchestrator>, IList<string>) input)
        {
            if (input.Item1 == null || input.Item2 == null)
                throw new ArgumentNullException(nameof(input));

            var allSupervisors = input.Item1;
            var scannedSupervisors = input.Item2;

            var runtimeStatusesToScan = new List<OrchestrationRuntimeStatus>
            {
                OrchestrationRuntimeStatus.Completed,
                OrchestrationRuntimeStatus.Failed,
                OrchestrationRuntimeStatus.Canceled,
                OrchestrationRuntimeStatus.Terminated,
                OrchestrationRuntimeStatus.Unknown
            };

            return allSupervisors
                .Where(i => !scannedSupervisors.Contains(i.InstanceId) &&
                    runtimeStatusesToScan.Contains(i.RuntimeStatus) &&
                    i.CustomStatus != null)
                .ToList();
        }
    }
}