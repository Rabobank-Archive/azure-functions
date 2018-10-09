using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using VstsLogAnalytics.Client;
using VstsLogAnalytics.Common;

namespace VstsLogAnalyticsFunction
{
    public static class PullRequestUpdatedToLogAnalytics
    {
        [FunctionName("PullRequestUpdatedToLogAnalytics")]
        public static async Task Run([QueueTrigger("pullrequestupdated", Connection = "connectionString")]string pullRequestEvent,
            [Inject]ILogAnalyticsClient lac, ILogger log)
        {
            try
            {
                log.LogInformation("logging pull request updated event to Log Analytics");

                await lac.AddCustomLogJsonAsync("pullrequest", JsonConvert.SerializeObject(new VstsToLogAnalyticsObjectMapper().GeneratePullRequestLog(pullRequestEvent)), "Date");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to write 'pull request updated event' to log analytics for event", pullRequestEvent);
            }
        }
    }
}