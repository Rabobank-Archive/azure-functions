using DurableFunctionsAdministration.Client.Response;

namespace CompletenessCheckFunction.Requests
{
    public class SingleAnalysisOrchestratorRequest
    {
        public OrchestrationInstance InstanceToAnalyze { get; set; }
    }
}