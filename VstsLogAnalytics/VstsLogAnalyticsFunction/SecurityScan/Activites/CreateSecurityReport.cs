using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SecurePipelineScan.Rules;
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

            var report = new SecurityReportScan(client);

            log.LogInformation($"Creating SecurityReport for project {project.Name}");

            var securityReport = report.Execute(project.Name);

            {
                try
                {
                    log.LogInformation($"Writing SecurityReport for project {project.Name} to Azure DevOps");

                    await logAnalyticsClient.AddCustomLogJsonAsync("SecurityScanReport", securityReport, "Date");
                }
                catch (Exception ex)
                {
                    log.LogError(ex, $"Failed to write report to log analytics: {ex}");
                    throw;
                }
            }
        }
    }
}