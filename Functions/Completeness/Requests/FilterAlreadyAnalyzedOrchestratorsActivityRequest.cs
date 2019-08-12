using System.Collections.Generic;
using Functions.Completeness.Responses;

namespace Functions.Completeness.Requests
{
    public class FilterAlreadyAnalyzedOrchestratorsActivityRequest
    {
        public IList<SimpleDurableOrchestrationStatus> InstancesToAnalyze { get; set; }
        public IList<string> InstanceIdsAlreadyAnalyzed { get; set; }
    }
}