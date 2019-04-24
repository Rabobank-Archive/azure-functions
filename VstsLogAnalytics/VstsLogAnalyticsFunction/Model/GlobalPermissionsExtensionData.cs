﻿using System;

namespace VstsLogAnalyticsFunction.Model
{
    public class GlobalPermissionsExtensionData : ExtensionDataReports<EvaluatedRule>
    {
        public DateTime Date { get; internal set; }
        public string RescanUrl { get; set; }
    }
}