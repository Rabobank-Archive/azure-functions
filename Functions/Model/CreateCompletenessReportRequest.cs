using System;
using System.Collections.Generic;
using Functions.Completeness.Model;

namespace Functions.Completeness.Requests
{
    public class CreateCompletenessReportRequest
    {
        public DateTime AnalysisCompleted { get; set; }
        public Orchestrator Supervisor { get; set; }
        public IList<Orchestrator> ProjectScanners { get; set; }
    }
}