using System;
using System.Collections.Generic;
using System.Text;

namespace VstsLogAnalytics.Client.LogAnalyticsModel
{
    public class RepositoryLog
    {
        public string Project { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string DefaultBranch { get; set; }

        public DateTime Date { get; set; }

    }
}
