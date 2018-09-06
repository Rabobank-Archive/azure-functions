using System;
using System.Collections.Generic;
using System.Text;

namespace VstsWebhookFunction.LogAnalyticsModel
{
    public class PullRequestLog
    {
        public string User { get; set; }
        public string UserEmail { get; set; }
        public DateTime Date { get; set; }
        public string TeamProject { get; set; }
        public string RepositoryId { get; set; }
        public string RepositoryName { get; set; }


        public string Title { get; set; }
        public int Id { get; set; }
        public string Status { get; set; }

        public string SourceBranch { get; set; }
        public string TargetBranch { get; set; }
        public int NumberOfReviewers { get; set; }
        public string Reviewers { get; set; }

        public string LastMergeSourceCommit { get; set; }
        public string LastMergeTargetCommit { get; set; }
        public string LastMergeCommit { get; set; }
        public string CommitIds { get; set; }


    }
}
