using System;
using System.Globalization;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VstsWebhookFunction.LogAnalyticsModel;

namespace VstsWebhookFunction
{
    public static class PullRequestCreatedToLogAnalytics
    {
        [FunctionName("PullRequestCreatedToLogAnalytics")]
        public static async void Run([QueueTrigger("pullrequestcreated", Connection = "connectionString")]string pullRequestCreatedEvent, ILogger log)
        {
            try
            {
                log.LogInformation("logging pull request created event to Log Analytics");

                LogAnalyticsClient lac = new LogAnalyticsClient(Environment.GetEnvironmentVariable("logAnalyticsWorkspace", EnvironmentVariableTarget.Process),
                                                                Environment.GetEnvironmentVariable("logAnalyticsKey", EnvironmentVariableTarget.Process));


                await lac.AddCustomLogJsonAsync("pullrequestcreated", JsonConvert.SerializeObject(GeneratePullRequestCreatedLog(pullRequestCreatedEvent)), "Date");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to write 'pull request created event' to log analytics for event", pullRequestCreatedEvent);
            }
        }

        public static PullRequestLog GeneratePullRequestCreatedLog(string pullRequestCreatedEvent)
        {
            PullRequestLog prcl = new PullRequestLog();
            dynamic data = JsonConvert.DeserializeObject(pullRequestCreatedEvent);
            if (data.eventType.ToString() != "git.pullrequest.created")
            {
                throw new ArgumentException($"Event data does not contain a valid json. Data {pullRequestCreatedEvent}");
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

            foreach (var commit in data.resource.commits)
            {
                prcl.CommitIds += $"{commit.commitId};";
            }

            return prcl;
        }
    }
}
