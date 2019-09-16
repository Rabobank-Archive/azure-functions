using SecurePipelineScan.VstsService.Response;
using System.Collections.Generic;

namespace Functions.Activities
{
    public class BuildPipelinesScanActivityRequest
    {
        public Project Project { get; set; }
        public BuildDefinition BuildDefinition { get; set; }
        public IList<string> CiIdentifiers { get; set; }
    }
}