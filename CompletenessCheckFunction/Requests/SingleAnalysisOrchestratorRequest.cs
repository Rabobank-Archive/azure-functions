using DurableFunctionsAdministration.Client.Response;
using System.Collections.Generic;

namespace CompletenessCheckFunction.Requests
{
    public class SingleAnalysisOrchestratorRequest
    {
        public OrchestrationInstance InstanceToAnalyze { get; set; }
        public List<OrchestrationInstance> AllProjectScanOrchestrators { get; set; }
    }
}