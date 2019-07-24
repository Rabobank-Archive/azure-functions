﻿using System.Collections.Generic;
using System.Linq;

namespace Functions.Model
{
    public class GlobalPermissionsExtensionData : ExtensionDataReports<EvaluatedRule>
    {
        public IEnumerable<PreventiveLogItem> Flatten()
        {
            return Reports.Select(rule => new PreventiveLogItem
            {
                Project = Id,
                Item = null,
                Rule = rule.Name,
                Status = rule.Status,
                EvaluatedDate = Date,
                Scope = RuleScopes.GlobalPermissions
            });
        }
    }
}
