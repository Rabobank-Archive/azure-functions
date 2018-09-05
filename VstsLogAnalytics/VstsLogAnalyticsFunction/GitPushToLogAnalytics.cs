using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VstsLogAnalyticsFunction;
using VstsWebhookFunction.LogAnalyticsModel;

namespace VstsLogAnalyticsFunction
{
    public static class GitPushToLogAnalytics
    {
        [FunctionName("GitPushToLogAnalytics")]
        public static async Task Run([QueueTrigger("codepushed", Connection = "connectionString")]string codePushedEvent, ILogger log)
        {
            try
            {
                log.LogInformation("logging code push to Log Analytics");

                LogAnalyticsClient lac = new LogAnalyticsClient(Environment.GetEnvironmentVariable("logAnalyticsWorkspace", EnvironmentVariableTarget.Process),
                                                                Environment.GetEnvironmentVariable("logAnalyticsKey", EnvironmentVariableTarget.Process));

                await lac.AddCustomLogJsonAsync("codepushed", JsonConvert.SerializeObject(new VstsToLogAnalyticsObjectMapper().GenerateCodePushLog(codePushedEvent)), "Date");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to write code push to log analytics for event", codePushedEvent);
            }
        }
    }
}
