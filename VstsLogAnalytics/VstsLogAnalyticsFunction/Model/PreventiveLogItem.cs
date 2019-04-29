using System;

namespace VstsLogAnalyticsFunction.Model
{
    public class PreventiveLogItem
    {
        public string Project { get; set; }
        public string Scope { get; set; }
        public string Item { get; set; }
        public string Rule { get; set; }
        public bool Status { get; set; }
        public DateTime EvaluatedDate { get; set; }
    }
}