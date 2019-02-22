using System.Collections.Generic;
using Rules.Reports;
using SecurePipelineScan.VstsService.Response;

namespace VstsLogAnalyticsFunction
{
    public class ExtensionDataReports<TReport> : ExtensionData
    {
        public IList<TReport> Reports { get; set; }
    }
}