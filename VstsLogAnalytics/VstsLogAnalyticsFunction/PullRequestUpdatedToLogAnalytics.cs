using System;
using System.Globalization;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
using VstsLogAnalyticsFunction;
using VstsWebhookFunction.LogAnalyticsModel;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.TeamFoundation.Build.WebApi;

namespace VstsLogAnalyticsFunction
{
    public static class PullRequestUpdatedToLogAnalytics
    {
        [FunctionName("PullRequestUpdatedToLogAnalytics")]
        public static async void Run([QueueTrigger("pullrequestupdated", Connection = "connectionString")]string pullRequestEvent, ILogger log)
        {
            try
            {
                log.LogInformation("logging pull request updated event to Log Analytics");

                LogAnalyticsClient lac = new LogAnalyticsClient(Environment.GetEnvironmentVariable("logAnalyticsWorkspace", EnvironmentVariableTarget.Process),
                                                                Environment.GetEnvironmentVariable("logAnalyticsKey", EnvironmentVariableTarget.Process));




                await lac.AddCustomLogJsonAsync("pullrequest", JsonConvert.SerializeObject(new VstsToLogAnalyticsObjectMapper().GeneratePullRequestLog(pullRequestEvent)), "Date");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to write 'pull request updated event' to log analytics for event", pullRequestEvent);
            }
        }
    }
}
