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
        public IList<Orchestrator> Run([ActivityTrigger] (Orchestrator, IList<Orchestrator>) input)
        {
            if (input.Item1 == null || input.Item2 == null)
                throw new ArgumentNullException(nameof(input));

            var supervisor = input.Item1;
            var allProjectScanners = input.Item2;

            return allProjectScanners
                .Where(i => OrchestrationHelper.GetSupervisorId(i.InstanceId) == supervisor.InstanceId)
                .ToList();
        }
    }
}