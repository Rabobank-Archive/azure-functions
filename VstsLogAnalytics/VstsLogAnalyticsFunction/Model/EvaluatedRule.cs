using System;
using System.Collections.Generic;
using System.Text;

namespace VstsLogAnalyticsFunction.Model
{
    public class EvaluatedRule
    {
        public string Description { get; set; }
        public bool Status { get; set; }
        public string ReconcileUrl { get; set; }
        public string Name { get; set; }
    }
}
