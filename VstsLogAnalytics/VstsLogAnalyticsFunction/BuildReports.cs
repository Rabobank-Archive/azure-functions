using System.Collections.Generic;
using SecurePipelineScan.Rules.Reports;
using SecurePipelineScan.VstsService.Response;

namespace VstsLogAnalyticsFunction
{
    public class BuildReports : ExtensionData
    {
        public IList<BuildScanReport> Reports { get; set; }
    }
}