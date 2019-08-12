using System.Collections.Generic;
using Functions.Completeness.Model;

namespace Functions.Completeness.Requests
{
    public class SingleAnalysisOrchestratorRequest
    {
        public SimpleDurableOrchestrationStatus InstanceToAnalyze { get; set; }
        public IList<SimpleDurableOrchestrationStatus> AllProjectScanOrchestrators { get; set; }
    }
}