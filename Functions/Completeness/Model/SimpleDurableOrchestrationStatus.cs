using Microsoft.Azure.WebJobs;
using Newtonsoft.Json.Linq;
using System;

namespace Functions.Completeness.Model
{
    public class SimpleDurableOrchestrationStatus
    {
        public string Name { get; set; }
        public string InstanceId { get; set; }
        public DateTime CreatedTime { get; set; }
        public OrchestrationRuntimeStatus RuntimeStatus { get; set; }
        public JToken CustomStatus { get; set; }
    }
}
