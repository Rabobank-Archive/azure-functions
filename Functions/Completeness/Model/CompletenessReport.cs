using System;

namespace Functions.Completeness.Model
{
    public class CompletenessReport
    {
        public DateTime AnalysisCompleted { get; set; }
        public string SupervisorId { get; set; }
        public DateTime SupervisorStarted { get; set; }
        public int? TotalProjectCount { get; set; }
        public int ScannedProjectCount { get; set; }
        public string FailedProjectIds { get; set; }
    }
}