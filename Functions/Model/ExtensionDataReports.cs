using SecurePipelineScan.VstsService.Response;
using System;
using System.Collections.Generic;

namespace Functions.Model
{
    public class ExtensionDataReports : ExtensionData
    {
        public DateTime Date { get; set; }
        public string RescanUrl { get; set; }
        public string HasReconcilePermissionUrl { get; set; }
    }

    public class ExtensionDataReports<TReport> : ExtensionDataReports
    {

        public IList<TReport> Reports { get; set; }
    }
}