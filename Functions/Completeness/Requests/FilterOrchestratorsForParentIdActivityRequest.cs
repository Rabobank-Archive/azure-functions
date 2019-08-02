using System.Collections.Generic;
using Microsoft.Azure.WebJobs;

namespace Functions.Completeness.Requests
{
    public class FilterOrchestratorsForParentIdActivityRequest
    {
        public string ParentId { get; set; }
        public IList<DurableOrchestrationStatus> InstancesToFilter { get; set; }
    }
}