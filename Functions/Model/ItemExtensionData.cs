using System.Collections.Generic;
using System.Linq;

namespace Functions.Model
{
    public class ItemsExtensionData : ExtensionDataReports<ItemExtensionData>
    {
        public IEnumerable<PreventiveLogItem> Flatten(string scope)
        {
            return 
                from report in Reports
                from rule in report.Rules
                select new PreventiveLogItem
                {
                    Project = Id, 
                    Scope = scope,
                    Item = report.Item,
                    Rule = rule.Name,
                    Status = rule.Status,
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