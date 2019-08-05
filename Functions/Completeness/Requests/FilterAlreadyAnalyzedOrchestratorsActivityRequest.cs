using System.Collections.Generic;
using Microsoft.Azure.WebJobs;

namespace Functions.Completeness.Requests
{
    public class FilterAlreadyAnalyzedOrchestratorsActivityRequest
    {
        public IList<DurableOrchestrationStatus> InstancesToAnalyze { get; set; }
        public IList<string> InstanceIdsAlreadyAnalyzed { get; set; }
    }
}