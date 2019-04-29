﻿namespace VstsLogAnalyticsFunction.Model
{
    public class EvaluatedRule
    {
        public string Description { get; set; }
        public bool Status { get; set; }
        public string Name { get; set; }
        public Reconcile Reconcile { get; set; }
    }

    public class Reconcile
    {
        public string Url { get; set; }
        public string[] Impact { get; set; }
    }
}
