using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SecurePipelineScan.Rules;
using SecurePipelineScan.Rules.Reports;
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
            [Inject] IProjectScan<SecurityReport> scan,
            ILogger log)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (logAnalyticsClient == null) throw new ArgumentNullException(nameof(logAnalyticsClient));
            if (scan == null) throw new ArgumentNullException(nameof(scan));

            var project = context.GetInput<Project>();
            if (project == null) throw new Exception("No Project found in parameter DurableActivityContextBase");

            log.LogInformation($"Creating SecurityReport for project {project.Name}");
            var report = scan.Execute(project.Name, DateTime.Now);

            try
            {
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