using System.Collections.Generic;
using Microsoft.Azure.WebJobs;

namespace Functions.Completeness.Requests
{
    public class SingleAnalysisOrchestratorRequest
    {
        public DurableOrchestrationStatus InstanceToAnalyze { get; set; }
        public IList<DurableOrchestrationStatus> AllProjectScanOrchestrators { get; set; }
    }
}