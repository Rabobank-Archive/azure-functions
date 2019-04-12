using SecurePipelineScan.VstsService.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace VstsLogAnalyticsFunction.Model
{
    public class GlobalPermissionsExtensionData : ExtensionData
    {
        public List<EvaluatedRule> EvaluatedRules { get; internal set; }
        public DateTime EvaluatedDate { get; internal set; }
    }
}
