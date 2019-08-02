using System;

namespace CompletenessCheckFunction.Requests
{
    public class UploadAnalysisResultToLogAnalyticsActivityRequest
    {
        public string SupervisorOrchestratorId { get; set; }
        public DateTime SupervisorStarted { get; set; } 
        public int TotalProjectCount { get; set; }
        public int ScannedProjectCount { get; set; }
        public DateTime AnalysisCompleted { get; set; }
    }
}