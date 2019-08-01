using System.Collections.Generic;
using System.Linq;
using static Functions.Helpers.OrchestrationIdHelper;

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
                ScanId = GetSupervisorId(instanceId), 
                Scope = RuleScopes.GlobalPermissions
            });
        }
    }
}
