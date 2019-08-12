using System;
using System.Collections.Generic;
using Functions.Completeness.Model;

namespace Functions.Completeness.Requests
{
    public class CreateAnalysisResultActivityRequest
    {
        public DateTime AnalysisCompleted { get; set; }
        public SimpleDurableOrchestrationStatus SupervisorOrchestrator { get; set; }
        public int TotalProjectCount { get; set; }
        public IList<SimpleDurableOrchestrationStatus> ProjectScanOrchestrators { get; set; }
    }
}