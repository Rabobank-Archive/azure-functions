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
    public class CreateSecurityReport
    {
        [FunctionName(nameof(CreateSecurityReport))]
        public static async Task Run(
            [ActivityTrigger] DurableActivityContext context,
            [Inject] ILogAnalyticsClient logAnalyticsClient,
            [Inject] IVstsRestClient client,
            ILogger log)
        {
            var project = context.GetInput<SecurePipelineScan.VstsService.Response.Project>();

            if (project == null) throw new ArgumentNullException("No project found from context while creating report");

            log.LogInformation($"Creating SecurityReport for project {project.Name}");

            var applicationGroups = client.Get(ApplicationGroup.ApplicationGroups(project.Id)).Identities;


            await AddToLogAnalytics(logAnalyticsClient, log, project, applicationGroups);
        }

        private static async Task AddToLogAnalytics(ILogAnalyticsClient logAnalyticsClient, ILogger log, Project project, IEnumerable<SecurePipelineScan.VstsService.Response.ApplicationGroup> applicationGroups)
        {
            try
            {
                var report = new
                {
                    Project = project.Name,
                    ApplicationGroupContainsProductionEnvironmentOwner = ProjectApplicationGroup.ApplicationGroupContainsProductionEnvironmentOwner(applicationGroups),
                    Date = DateTime.UtcNow,
                };
                log.LogInformation($"Writing SecurityReport for project {project.Name} to Azure Devops");

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