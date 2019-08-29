using System;
using System.Collections.Generic;
using System.Linq;
using Functions.Completeness.Requests;
using Functions.Completeness.Model;
using Microsoft.Azure.WebJobs;

namespace Functions.Completeness.Activities
{
    public class FilterSupervisorsActivity
    {
        [FunctionName(nameof(FilterSupervisorsActivity))]
        public IList<Orchestrator> Run([ActivityTrigger] FilterSupervisorsRequest request)
        {
            var runtimeStatusesToScan = new List<OrchestrationRuntimeStatus>
            {
                OrchestrationRuntimeStatus.Completed,
                OrchestrationRuntimeStatus.Failed,
                OrchestrationRuntimeStatus.Canceled,
                OrchestrationRuntimeStatus.Terminated,
                OrchestrationRuntimeStatus.Unknown
            };

            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return request.AllSupervisors
                .Where(i => !request.ScannedSupervisors.Contains(i.InstanceId) &&
                    runtimeStatusesToScan.Contains(i.RuntimeStatus) &&
                    i.CustomStatus != null)
                .ToList();
        }
    }
}