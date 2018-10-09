using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using VstsLogAnalytics.Client;
using VstsLogAnalytics.Common;

namespace VstsLogAnalyticsFunction
{
    public static class BranchPoliciesFunction
    {
        [FunctionName("BranchPoliciesFunction")]
        public static async Task Run([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer,
            [Inject]ILogAnalyticsClient laClient,
            [Inject]IVstsHttpClient vstsClient,
            ILogger log)
        {
            try
            {
                log.LogInformation($"Branch Policies timed check start: {DateTime.Now}");

                var logDate = DateTime.UtcNow;

                var repos = await vstsClient.GetRepositoriesForTeamProject("TAS");

                var repolog = new VstsToLogAnalyticsObjectMapper().GenerateReposLog(repos, logDate);
                await laClient.AddCustomLogJsonAsync("gitrepo", JsonConvert.SerializeObject(repolog), "Date");

                var policies = await vstsClient.GetRepoPoliciesForTeamProject("TAS");
                var policylog = new VstsToLogAnalyticsObjectMapper().GeneratePolicyConfigurationLog(policies, logDate, repolog);

                await laClient.AddCustomLogJsonAsync("gitpolicy", JsonConvert.SerializeObject(policylog), "Date");

                log.LogInformation($"Branch Policies timed check end: {DateTime.Now}");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to write branch policies to log analytics", myTimer);
            }
        }
    }
}