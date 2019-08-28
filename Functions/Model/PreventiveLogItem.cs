using System;

namespace Functions.Model
{
    public class PreventiveLogItem
    {
        public string Project { get; set; }
        public string ProjectId { get; set; }
        public string Scope { get; set; }
        public string Item { get; set; }
        public string Rule { get; set; }
        public bool Status { get; set; }
        public bool IsSox { get; set; }
        public DateTime EvaluatedDate { get; set; }
        public string ScanId { get; set; }
    }
}