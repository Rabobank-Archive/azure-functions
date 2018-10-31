using System;

namespace VstsLogAnalyticsFunction
{
    internal class ReleaseDeployment
    {
        public int ReleaseId { get; set; }
        public int EnvironmentId { get; set; }
        public string ProjectName { get; set; }
        public bool FourEyesOnAllBuildArtefacts { get; set; }
        public bool LastModifiedByNotTheSameAsApprovedBy { get; set; }
        public DateTime Date { get; set; }
    }
}