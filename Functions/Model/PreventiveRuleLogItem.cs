using System;

namespace Functions.Model
{
    public class PreventiveRuleLogItem
    {
        public string Project { get; set; }
        public string ProjectId { get; set; }
        public string Scope { get; set; }
        public string Item { get; set; }
        public string ItemId { get; set; }
        public string Rule { get; set; }
        public bool? Status { get; set; }
        public bool IsSox { get; set; }
        public DateTime EvaluatedDate { get; set; }
        public string ScanId { get; set; }
        public string CiIdentifiers { get; set; }
    }
}