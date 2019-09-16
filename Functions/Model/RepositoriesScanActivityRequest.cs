using SecurePipelineScan.VstsService.Response;
using System.Collections.Generic;

namespace Functions.Activities
{
    public class RepositoriesScanActivityRequest
    {
        public Project Project { get; set; }
        public Repository Repository { get; set; }
        public IList<string> CiIdentifiers { get; set; }
    }
}