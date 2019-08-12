using System.Collections.Generic;
using Functions.Completeness.Model;

namespace Functions.Completeness.Requests
{
    public class SingleCompletenessCheckRequest
    {
        public Orchestrator Supervisor { get; set; }
        public IList<Orchestrator> AllProjectScanners { get; set; }
    }
}