using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using VstsWebhookFunction.LogAnalyticsModel;

namespace VstsLogAnalyticsFunction
{
    public class VstsToLogAnalyticsObjectMapper
    {
        public PullRequestLog GeneratePullRequestLog(string pullRequestEvent)
        {
            PullRequestLog prcl = new PullRequestLog();
            dynamic data = JsonConvert.DeserializeObject(pullRequestEvent);
            if (data.eventType.ToString() == "git.pullrequest.created" || 
                data.eventType.ToString() == "git.pullrequest.updated")
            {
                throw new ArgumentException($"Event data does not contain a valid json. Data {pullRequestEvent}");
            }

            prcl.User = data.resource.createdBy.displayName;
            prcl.UserEmail = data.resource.createdBy.uniqueName;

            prcl.Date = data.resource.creationDate;
            prcl.RepositoryId = data.resource.repository.id;
            prcl.RepositoryName = data.resource.repository.name;

            prcl.TeamProject = data.resource.repository.project.name;

            prcl.LastMergeSourceCommit = data.resource?.lastMergeSourceCommit?.commitId;
            prcl.LastMergeTargetCommit = data.resource?.lastMergeTargetCommit?.commitId;
            prcl.LastMergeCommit = data.resource?.lastMergeCommit?.commitId;
            prcl.NumberOfReviewers = data.resource.reviewers.Count;

            foreach (var reviewer in data.resource.reviewers)
            {
                prcl.Reviewers += $"{reviewer.displayName};";
            }

            prcl.SourceBranch = data.resource.sourceRefName;
            prcl.TargetBranch = data.resource.targetRefName;
            prcl.Title = data.resource.title;
            prcl.Status = data.resource.status;
            prcl.Id = data.resource.pullRequestId;

            if (data.resource?.commits != null)
            {
                foreach (var commit in data.resource.commits)
                {
                    prcl.CommitIds += $"{commit.commitId};";
                }
            }

            return prcl;
        }

        public CodePushedLog GenerateCodePushLog(string codePushedEvent)
        {
            CodePushedLog cpl = new CodePushedLog();
            dynamic data = JsonConvert.DeserializeObject(codePushedEvent);
            if (data.eventType.ToString() != "git.push")
            {
                throw new ArgumentException($"Event data does not contain a valid json. Data {codePushedEvent}");
            }

            cpl.User = data.resource.pushedBy.displayName;
            cpl.UserEmail = data.resource.pushedBy.uniqueName;
            cpl.Date = data.resource.date;
            cpl.RepositoryId = data.resource.repository.id;
            cpl.RepositoryName = data.resource.repository.name;
            cpl.TeamProject = data.resource.repository.project.name;
            cpl.VstsCommitId = data.resource.refUpdates[0].newObjectId;
            cpl.Branch = data.resource.refUpdates[0].name;

            return cpl;
        }


    }
}
