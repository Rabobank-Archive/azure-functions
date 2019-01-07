using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SecurePipelineScan.Rules.Checks;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;
using VstsLogAnalytics.Client;
using VstsLogAnalytics.Common;
using Project = SecurePipelineScan.VstsService.Response.Project;

namespace VstsLogAnalyticsFunction.SecurityScan.Activites
{
    public static class CreateSecurityReport
    {
        [FunctionName(nameof(CreateSecurityReport))]
        public static async Task Run(
            [ActivityTrigger] DurableActivityContextBase context,
            [Inject] ILogAnalyticsClient logAnalyticsClient,
            [Inject] IVstsRestClient client,
            ILogger log)
        {
            var project = context.GetInput<Project>();

            if (project == null) throw new Exception("No Project found in parameter DurableActivityContextBase");

            log.LogInformation($"Creating SecurityReport for project {project.Name}");

            var applicationGroups = client.Get(ApplicationGroup.ApplicationGroups(project.Id)).Identities;

            await AddToLogAnalytics(logAnalyticsClient, log, project, applicationGroups);
        }

        private static async Task AddToLogAnalytics(ILogAnalyticsClient logAnalyticsClient, ILogger log, Project project,
            IEnumerable<SecurePipelineScan.VstsService.Response.ApplicationGroup> applicationGroups)
        {
            try
            {
                var report = new
                {
                    Project = project.Name,
                    ApplicationGroupContainsProductionEnvironmentOwner = ProjectApplicationGroup.ApplicationGroupContainsProductionEnvironmentOwner(applicationGroups),
                    Date = DateTime.UtcNow,
                };
                log.LogInformation($"Writing SecurityReport for project {project.Name} to Azure DevOps");

                await logAnalyticsClient.AddCustomLogJsonAsync("SecurityScanReport", report, "Date");
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Failed to write report to log analytics: {ex}");
                throw;
            }
        }
    }
}