using System.Collections.Generic;
using DurableFunctionsAdministration.Client.Response;

namespace CompletenessCheckFunction.Requests
{
    public class FilterAlreadyAnalyzedOrchestratorsActivityRequest
    {
        public IList<OrchestrationInstance> InstancesToAnalyze = new List<OrchestrationInstance>();
        public IList<string> InstanceIdsAlreadyAnalyzed = new List<string>();
    }
}