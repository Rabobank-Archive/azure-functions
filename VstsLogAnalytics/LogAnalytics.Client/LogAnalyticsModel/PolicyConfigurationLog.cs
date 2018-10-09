using System;
using System.Collections.Generic;
using System.Text;

namespace VstsLogAnalytics.Client.LogAnalyticsModel
{
    public class PolicyConfigurationLog
    {
        public string Id { get; set; }
    
        public int Version { get; set; }

        public string RepositoryId { get; set; }

        public string Branch { get; set; }

        public DateTime Date { get; set; }

        public int MinimumApproverCount { get; set; }

        public bool CreatorVoteCounts { get; set; }

        public bool IsEnabled { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsBlocking { get; set; }

        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }

    }
}
