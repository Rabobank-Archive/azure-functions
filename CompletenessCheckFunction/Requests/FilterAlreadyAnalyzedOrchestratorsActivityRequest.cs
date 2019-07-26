using System.Collections.Generic;
using DurableFunctionsAdministration.Client.Response;

namespace CompletenessCheckFunction.Requests
{
    public class FilterAlreadyAnalyzedOrchestratorsActivityRequest
    {
        public List<OrchestrationInstance> InstancesToAnalyze = new List<OrchestrationInstance>();
        public List<string> InstanceIdsAlreadyAnalyzed = new List<string>();
    }
}