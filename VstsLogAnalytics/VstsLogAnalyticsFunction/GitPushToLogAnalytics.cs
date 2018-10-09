using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using VstsLogAnalytics.Client;
using VstsLogAnalytics.Common;

namespace VstsLogAnalyticsFunction
{
    public static class GitPushToLogAnalytics
    {
        [FunctionName("GitPushToLogAnalytics")]
        public static async Task Run(
            [QueueTrigger("codepushed", Connection = "connectionString")]
        string codePushedEvent,
            [Inject]ILogAnalyticsClient lac,
            ILogger log)
        {
            try
            {
                log.LogInformation("logging code push to Log Analytics");

                await lac.AddCustomLogJsonAsync("codepushed", JsonConvert.SerializeObject(new VstsToLogAnalyticsObjectMapper().GenerateCodePushLog(codePushedEvent)), "Date");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to write code push to log analytics for event", codePushedEvent);
            }
        }
    }
}