using System.Collections.Generic;
using System.Linq;
using Functions.Model;
using Functions.Helpers;
using Microsoft.Azure.WebJobs;
using System;

namespace Functions.Activities
{
    public class FilterProjectScannersActivity
    {
        [FunctionName(nameof(FilterProjectScannersActivity))]
        public IList<Orchestrator> Run([ActivityTrigger] (Orchestrator, IList<Orchestrator>) data)
        {
            if (data.Item1 == null || data.Item2 == null)
                throw new ArgumentNullException(nameof(data));

            var supervisor = data.Item1;
            var allProjectScanners = data.Item2;

            return allProjectScanners
                .Where(i => OrchestrationHelper.GetSupervisorId(i.InstanceId) == supervisor.InstanceId)
                .ToList();
        }
    }
}