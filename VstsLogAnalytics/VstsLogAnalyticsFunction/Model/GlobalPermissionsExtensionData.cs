using System;
using System.Collections.Generic;
using System.Linq;

namespace VstsLogAnalyticsFunction.Model
{
    public class GlobalPermissionsExtensionData : ExtensionDataReports<EvaluatedRule>
    {
        public DateTime Date { get; set; }
        public string RescanUrl { get; set; }
        public string HasReconcilePermissionUrl { get; set; }

        public IEnumerable<PreventiveLogItem> Flatten()
        {
            return Reports.Select(rule => new PreventiveLogItem
            {
                Project = Id,
                Item = null,
                Rule = rule.Name,
                Status = rule.Status,
                EvaluatedDate = Date,
                Scope = "globalpermissions"
            });
        }
    }
}
