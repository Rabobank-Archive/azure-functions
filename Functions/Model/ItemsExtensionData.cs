using System;
using System.Collections.Generic;
using System.Linq;
using static Functions.Helpers.OrchestrationHelper;

namespace Functions.Model
{
    public class ItemsExtensionData : ExtensionDataReports<ItemExtensionData>
    {
        public IEnumerable<PreventiveRuleLogItem> Flatten(
            string scope, string instanceId, string projectId, DateTime scanDate)
        {
            return
                from report in Reports
                from rule in report.Rules
                select new PreventiveRuleLogItem
                {
                    EvaluatedDate = Date,
                    ScanDate = scanDate,
                    ScanId = GetSuperVisorIdForScopeOrchestrator(instanceId),
                    Project = Id,
                    ProjectId = projectId,
                    Scope = scope,
                    Item = report.Item,
                    ItemId = report.ItemId,
                    CiIdentifiers = report.CiIdentifiers,
                    Rule = rule.Name,
                    IsSox = rule.IsSox,
                    Status = rule.Status
                };
        }
    }
}