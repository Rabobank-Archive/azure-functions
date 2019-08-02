using System.Collections.Generic;
using DurableFunctionsAdministration.Client.Response;

namespace CompletenessCheckFunction.Requests
{
    public class FilterAlreadyAnalyzedOrchestratorsActivityRequest
    {
        public IList<OrchestrationInstance> InstancesToAnalyze { get; set; }
        public IList<string> InstanceIdsAlreadyAnalyzed { get; set; }
    }
}