using System.Collections.Generic;
using System.Linq;
using static Functions.Helpers.OrchestrationIdHelper;

namespace Functions.Model
{
    public class ItemsExtensionData : ExtensionDataReports<ItemExtensionData>
    {
        public IEnumerable<PreventiveLogItem> Flatten(string scope, string instanceId)
        {
            return
                from report in Reports
                from rule in report.Rules
                select new PreventiveLogItem
                {
                    Project = Id,
                    ProjectId = GetProjectId(instanceId),
                    Scope = scope,
                    Item = report.Item,
                    Rule = rule.Name,
                    IsSox = rule.IsSox,
                    Status = rule.Status,
                    ScanId = GetSupervisorId(instanceId), 
                    EvaluatedDate = Date
                };
        }
    }

    public class ItemExtensionData
    {
        public string Item { get; set; }
        public IList<EvaluatedRule> Rules { get; set; }
    }
}