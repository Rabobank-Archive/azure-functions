using System.Collections.Generic;
using SecurePipelineScan.VstsService.Response;

namespace Functions
{
    public class ExtensionDataReports<TReport> : ExtensionData
    {
        
        public IList<TReport> Reports { get; set; }
    }
}