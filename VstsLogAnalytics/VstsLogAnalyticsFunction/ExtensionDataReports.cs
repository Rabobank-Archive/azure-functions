using System.Collections.Generic;
using Rules.Reports;
using SecurePipelineScan.VstsService.Response;

namespace VstsLogAnalyticsFunction
{
    public class ExtensionDataReports : ExtensionData
    {
        public IList<RepositoryReport> Reports { get; set; }
    }
}