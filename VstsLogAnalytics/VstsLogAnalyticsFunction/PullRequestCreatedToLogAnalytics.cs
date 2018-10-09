using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using VstsLogAnalytics.Client;
using VstsLogAnalytics.Common;

namespace VstsLogAnalyticsFunction
{
    public static class PullRequestCreatedToLogAnalytics
    {
        [FunctionName("PullRequestCreatedToLogAnalytics")]
        public static async Task Run(
            [QueueTrigger("pullrequestcreated", Connection = "connectionString")]
        string pullRequestEvent, ILogger log, [Inject]ILogAnalyticsClient lac)
        {
            try
            {
                log.LogInformation("logging pull request created event to Log Analytics");

                await lac.AddCustomLogJsonAsync("pullrequest", JsonConvert.SerializeObject(new VstsToLogAnalyticsObjectMapper().GeneratePullRequestLog(pullRequestEvent)), "Date");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to write 'pull request created event' to log analytics for event", pullRequestEvent);
            }
        }
    }
}