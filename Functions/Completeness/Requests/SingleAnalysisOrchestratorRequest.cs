using System.Collections.Generic;
using Functions.Completeness.Responses;

namespace Functions.Completeness.Requests
{
    public class SingleAnalysisOrchestratorRequest
    {
        public SimpleDurableOrchestrationStatus InstanceToAnalyze { get; set; }
        public IList<SimpleDurableOrchestrationStatus> AllProjectScanOrchestrators { get; set; }
    }
}