using System;

namespace Functions.Model
{
    public class ReleaseLogItem
    {
        public int ReleaseId { get; set; }
        public bool Approved { get; set; }
        public string CiIdentifier { get; set; }
        public string CiName { get; set; }
        public Uri ReleaseLink { get; set; }
        public string ReleasePipelineId { get; set; }
        public string ReleaseStageId { get; set; }
    }
}