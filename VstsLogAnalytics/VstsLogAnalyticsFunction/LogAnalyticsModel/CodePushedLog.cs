using System;
using System.Collections.Generic;
using System.Text;

namespace VstsWebhookFunction.LogAnalyticsModel
{
    public class CodePushedLog
    {
        public string User { get; set; }
        public string UserEmail { get; set; }
        public DateTime Date { get; set; }
        public string TeamProject { get; set; }
        public string RepositoryId { get; set; }
        public string RepositoryName { get; set; }
        public string VstsCommitId { get; set; }
    }
}
