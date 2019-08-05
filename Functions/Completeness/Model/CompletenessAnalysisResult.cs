using System;

namespace Functions.Completeness.Model
{
    public class CompletenessAnalysisResult
    {
        public DateTime AnalysisCompleted { get; set; }
        public string SupervisorOrchestratorId { get; set; }
        public DateTime SupervisorStarted { get; set; }
        public int TotalProjectCount { get; set; }
        public int ScannedProjectCount { get; set; }
    }
}