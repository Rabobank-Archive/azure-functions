using Microsoft.TeamFoundation.Policy.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using VstsLogAnalytics.Client.LogAnalyticsModel;

namespace VstsLogAnalytics.Client
{
    public class VstsToLogAnalyticsObjectMapper
    {
        public PullRequestLog GeneratePullRequestLog(string pullRequestEvent)
        {
            dynamic data = JsonConvert.DeserializeObject(pullRequestEvent);
            if (data.eventType.ToString() != "git.pullrequest.created" &&
                data.eventType.ToString() != "git.pullrequest.updated")
            {
                throw new ArgumentException($"Event data does not contain a valid json. Data {pullRequestEvent}");
            }

            PullRequestLog prcl = new PullRequestLog
            {
                User = data.resource.createdBy.displayName,
                UserEmail = data.resource.createdBy.uniqueName,

                Date = data.resource.creationDate,
                RepositoryId = data.resource.repository.id,
                RepositoryName = data.resource.repository.name,

                TeamProject = data.resource.repository.project.name,

                LastMergeSourceCommit = data.resource?.lastMergeSourceCommit?.commitId,
                LastMergeTargetCommit = data.resource?.lastMergeTargetCommit?.commitId,
                LastMergeCommit = data.resource?.lastMergeCommit?.commitId,
                NumberOfReviewers = data.resource.reviewers.Count
            };

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

        public IEnumerable<RepositoryLog> GenerateReposLog(List<GitRepository> repos, DateTime date)
        {
            foreach (var repo in repos)
            {
                yield return new RepositoryLog
                {
                    Name = repo.Name,
                    Id = repo.Id.ToString(),
                    Project = repo.ProjectReference.Name,
                    DefaultBranch = repo.DefaultBranch,
                    Date = date
                };
            }
        }

        public CodePushedLog GenerateCodePushLog(string codePushedEvent)
        {
            dynamic data = JsonConvert.DeserializeObject(codePushedEvent);
            if (data.eventType.ToString() != "git.push")
            {
                throw new ArgumentException($"Event data does not contain a valid json. Data {codePushedEvent}");
            }
            CodePushedLog cpl = new CodePushedLog
            {
                User = data.resource.pushedBy.displayName,
                UserEmail = data.resource.pushedBy.uniqueName,
                Date = data.resource.date,
                RepositoryId = data.resource.repository.id,
                RepositoryName = data.resource.repository.name,
                TeamProject = data.resource.repository.project.name,
                VstsCommitId = data.resource.refUpdates[0].newObjectId,
                Branch = data.resource.refUpdates[0].name
            };

            return cpl;
        }

        public IEnumerable<PolicyConfigurationLog> GeneratePolicyConfigurationLog(IEnumerable<PolicyConfiguration> policies, DateTime date, IEnumerable<RepositoryLog> repos)
        {
            foreach (var policy in policies)
            {
                dynamic settings = policy.Settings;

                PolicyConfigurationLog log = new PolicyConfigurationLog
                {
                    Id = policy.Id.ToString(),
                    Branch = settings.scope[0].refName,
                    CreatorVoteCounts = settings?.creatorVoteCounts ?? false,
                    Date = date,
                    MinimumApproverCount = settings?.minimumApproverCount ?? 0,
                    Version = policy.Revision,
                    IsBlocking = policy.IsBlocking,
                    IsEnabled = policy.IsEnabled,
                    IsDeleted = policy.IsDeleted,
                    CreatedDate = policy.CreatedDate,
                    CreatedBy = policy.CreatedBy.UniqueName
                };
                if (settings.scope[0].repositoryId != null)
                {
                    log.RepositoryId = settings.scope[0].repositoryId;
                }

                yield return log;
            }
        }
    }
}