using SecurePipelineScan.Rules.Reports;
using System;
using System.Collections.Generic;

namespace VstsLogAnalyticsFunction
{
    public class EndPointReport
    {
        public List<ScanReport> Reports { get; set; }

        public DateTime Date { get; set; }

        public EndPointReport()
        {
            Reports = new List<ScanReport>();
        }
    }
}