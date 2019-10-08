using System.Collections.Generic;
using Functions.Completeness.Model;

namespace Functions.Completeness.Requests
{
    public class FilterSupervisorsRequest
    {
        public IList<Orchestrator> AllSupervisors { get; set; }
        public IList<string> ScannedSupervisors { get; set; }
    }
}