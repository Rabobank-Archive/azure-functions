using System.Collections.Generic;
using System.Linq;

namespace Functions.Model
{
    public class GlobalPermissionsExtensionData : ExtensionDataReports<EvaluatedRule>
    {
        public IEnumerable<PreventiveLogItem> Flatten(string instanceId)
        {            
            return Reports.Select(rule => new PreventiveLogItem
            {
                Project = Id,
                Item = null,
                Rule = rule.Name,
                Status = rule.Status,
                EvaluatedDate = Date,
                ScanId = instanceId.Split(':')[0],
                Scope = RuleScopes.GlobalPermissions
            });
        }
    }
}
