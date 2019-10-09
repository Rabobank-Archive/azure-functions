using System.Collections.Generic;
using System.Linq;
using static Functions.Helpers.OrchestrationHelper;

namespace Functions.Model
{
    public class ItemsExtensionData : ExtensionDataReports<ItemExtensionData>
    {
        public IEnumerable<PreventiveRuleLogItem> Flatten(string scope, string instanceId)
        {
            return
                from report in Reports
                from rule in report.Rules
                select new PreventiveRuleLogItem
                {
                    Project = Id,
                    ProjectId = GetProjectId(instanceId),
                    Scope = scope,
                    Item = report.Item,
                    ItemId = report.ItemId,
                    Rule = rule.Name,
                    IsSox = rule.IsSox,
                    Status = rule.Status,
                    ScanId = GetSupervisorId(instanceId), 
                    EvaluatedDate = Date,
                    CiIdentifiers = report.CiIdentifiers
                };
        }
    }
}