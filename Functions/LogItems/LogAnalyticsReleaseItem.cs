using System;

namespace Functions
{
    public class LogAnalyticsReleaseItem
    {
        public string Endpoint { get; set; }
        public string EndpointType { get; set; }
        public string Definition { get; set; }
        public int RequestId { get; set; }
        public string StageName { get; set; }
        public bool? HasFourEyesOnAllBuildArtefacts { get; set; }
        public DateTime Date { get; set; }
    }
}