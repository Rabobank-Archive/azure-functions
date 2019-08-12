using System.Collections.Generic;
using Functions.Completeness.Model;

namespace Functions.Completeness.Requests
{
    public class FilterAlreadyAnalyzedOrchestratorsActivityRequest
    {
        public IList<SimpleDurableOrchestrationStatus> InstancesToAnalyze { get; set; }
        public IList<string> InstanceIdsAlreadyAnalyzed { get; set; }
    }
}