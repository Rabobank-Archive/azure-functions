using System;

namespace VstsLogAnalyticsFunction
{
    internal class LogAnalyticsReleaseItem
    {
        public string Endpoint { get; set; }
        public string Definition { get; set; }
        public int RequestId { get; set; }
        public string OwnerName { get; set; }
        public bool? HasFourEyesOnAllBuildArtefacts { get; set; }
        public DateTime Date { get; set; }
    }
}