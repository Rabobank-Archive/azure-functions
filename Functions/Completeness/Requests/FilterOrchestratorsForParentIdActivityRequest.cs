using System.Collections.Generic;
using Functions.Completeness.Responses;

namespace Functions.Completeness.Requests
{
    public class FilterOrchestratorsForParentIdActivityRequest
    {
        public string ParentId { get; set; }
        public IList<SimpleDurableOrchestrationStatus> InstancesToFilter { get; set; }
    }
}