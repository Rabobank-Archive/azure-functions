using SecurePipelineScan.VstsService.Response;
using System;
using System.Collections.Generic;

namespace Functions.Model
{
    public class ExtensionDataReports : ExtensionData
    {
        public DateTime Date { get; set; }
        public Uri RescanUrl { get; set; }
        public Uri HasReconcilePermissionUrl { get; set; }
    }

    public class ExtensionDataReports<TReport> : ExtensionDataReports
    {

        public IList<TReport> Reports { get; set; }
    }
}