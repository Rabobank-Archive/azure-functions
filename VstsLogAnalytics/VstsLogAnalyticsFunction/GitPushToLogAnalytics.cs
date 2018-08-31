using System;
using System.Globalization;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VstsWebhookFunction.LogAnalyticsModel;

namespace VstsWebhookFunction
{
    public static class GitPushToLogAnalytics
    {
        [FunctionName("GitPushToLogAnalytics")]
        public static async void Run([QueueTrigger("codepushed", Connection = "connectionString")]string codePushedEvent, ILogger log)
        {
            try
            {
                log.LogInformation("logging code push to Log Analytics");

                LogAnalyticsClient lac = new LogAnalyticsClient(Environment.GetEnvironmentVariable("logAnalyticsWorkspace", EnvironmentVariableTarget.Process),
                                                                Environment.GetEnvironmentVariable("logAnalyticsKey", EnvironmentVariableTarget.Process));


                await lac.AddCustomLogJsonAsync("codepushed", JsonConvert.SerializeObject(GenerateCodePushLog(codePushedEvent)), "Date");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to write code push to log analytics for event", codePushedEvent);
            }
        }

        public static CodePushedLog GenerateCodePushLog(string codePushedEvent)
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

            return cpl;
        }
    }
}
