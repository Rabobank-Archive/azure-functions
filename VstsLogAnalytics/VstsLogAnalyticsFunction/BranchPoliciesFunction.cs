using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace VstsLogAnalyticsFunction
{
    public static class BranchPoliciesFunction
    {
        [FunctionName("BranchPoliciesFunction")]
        public static async void Run([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer, ILogger log)
        {
            try
            {
                log.LogInformation($"Branch Policies timed check start: {DateTime.Now}");

                LogAnalyticsClient lac = new LogAnalyticsClient(Environment.GetEnvironmentVariable("logAnalyticsWorkspace", EnvironmentVariableTarget.Process),
                                                    Environment.GetEnvironmentVariable("logAnalyticsKey", EnvironmentVariableTarget.Process));

                VstsHttpClient vstsClient = new VstsHttpClient(Environment.GetEnvironmentVariable("vstsUrl", EnvironmentVariableTarget.Process),
                                               Environment.GetEnvironmentVariable("vstsPat", EnvironmentVariableTarget.Process));

                var repos = await vstsClient.GetRepositoriesForTeamProject("TAS");

                await lac.AddCustomLogJsonAsync("gitrepo", JsonConvert.SerializeObject(new VstsToLogAnalyticsObjectMapper().GenerateReposLog(repos,DateTime.Now)), "Date");


                log.LogInformation($"Branch Policies timed check end: {DateTime.Now}");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to write branch policies to log analytics",myTimer);
            }
        }
    }
}
