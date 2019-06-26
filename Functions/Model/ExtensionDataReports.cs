using System;
using System.Collections.Generic;
using SecurePipelineScan.VstsService.Response;

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