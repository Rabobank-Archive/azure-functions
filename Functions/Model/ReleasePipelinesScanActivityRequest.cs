using SecurePipelineScan.VstsService.Response;
using System.Collections.Generic;

namespace Functions.Activities
{
    public class ReleasePipelinesScanActivityRequest
    {
        public Project Project { get; set; }
        public ReleaseDefinition ReleaseDefinition { get; set; }
        public IList<string> CiIdentifiers { get; set; }
    }
}