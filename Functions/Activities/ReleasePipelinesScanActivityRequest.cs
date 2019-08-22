using SecurePipelineScan.VstsService.Response;

namespace Functions.Activities
{
    public class ReleasePipelinesScanActivityRequest
    {
        public Project Project { get; set; }
        public ReleaseDefinition ReleaseDefinition { get; set; }
    }
}