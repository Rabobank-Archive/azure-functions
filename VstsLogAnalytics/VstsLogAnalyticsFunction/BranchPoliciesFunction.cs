using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace VstsLogAnalyticsFunction
{
    public static class BranchPoliciesFunction
    {
        [FunctionName("BranchPoliciesFunction")]
        public static async Task Run([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer, ILogger log)
        {
            try
            {
                log.LogInformation($"Branch Policies timed check start: {DateTime.Now}");

                LogAnalyticsClient lac = new LogAnalyticsClient(Environment.GetEnvironmentVariable("logAnalyticsWorkspace", EnvironmentVariableTarget.Process),
                                                    Environment.GetEnvironmentVariable("logAnalyticsKey", EnvironmentVariableTarget.Process));

                VstsHttpClient vstsClient = new VstsHttpClient(Environment.GetEnvironmentVariable("vstsUrl", EnvironmentVariableTarget.Process),
                                               Environment.GetEnvironmentVariable("vstsPat", EnvironmentVariableTarget.Process));

                var logDate = DateTime.Now;

                var repos = await vstsClient.GetRepositoriesForTeamProject("TAS");

                var repolog = new VstsToLogAnalyticsObjectMapper().GenerateReposLog(repos,logDate);
                await lac.AddCustomLogJsonAsync("gitrepo", JsonConvert.SerializeObject(repolog), "Date");



                var policies = await vstsClient.GetRepoPoliciesForTeamProject("TAS");
                var policylog = new VstsToLogAnalyticsObjectMapper().GeneratePolicyConfigurationLog(policies, logDate, repolog);

                await lac.AddCustomLogJsonAsync("gitpolicy", JsonConvert.SerializeObject(policylog), "Date");


                log.LogInformation($"Branch Policies timed check end: {DateTime.Now}");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to write branch policies to log analytics",myTimer);
            }
        }
    }
}
