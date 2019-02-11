using System.Collections.Generic;
using SecurePipelineScan.Rules.Reports;
using SecurePipelineScan.VstsService.Response;

namespace VstsLogAnalyticsFunction
{
    public class ReleaseReports : ExtensionData
    {
        public IList<ReleaseDeploymentCompletedReport> Reports { get; set; }
    }
}