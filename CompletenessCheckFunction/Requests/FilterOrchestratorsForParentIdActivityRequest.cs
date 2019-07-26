using System.Collections.Generic;
using DurableFunctionsAdministration.Client.Response;

namespace CompletenessCheckFunction.Requests
{
    public class FilterOrchestratorsForParentIdActivityRequest
    {
        public string ParentId { get; set; }
        public IList<OrchestrationInstance> InstancesToFilter { get; set; }
    }
}