using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SecurePipelineScan.Rules;
using SecurePipelineScan.Rules.Reports;
using VstsLogAnalytics.Client;
using VstsLogAnalytics.Common;
using Project = SecurePipelineScan.VstsService.Response.Project;

namespace VstsLogAnalyticsFunction
{
    public  class SecurityScanProjectActivity
    {
        private readonly ILogAnalyticsClient _client;
        private readonly IProjectScan<SecurityReport> _scan;

        public SecurityScanProjectActivity(ILogAnalyticsClient client,
            IProjectScan<SecurityReport> scan)
        {
            _client = client;
            _scan = scan;
        }

        [FunctionName(nameof(SecurityScanProjectActivity))]
        public async Task Run(
            [ActivityTrigger] DurableActivityContextBase context,
            ILogger log)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var project = context.GetInput<Project>();
            if (project == null) throw new Exception("No Project found in parameter DurableActivityContextBase");

            log.LogInformation($"Creating SecurityReport for project {project.Name}");
            var dateTimeUtcNow = DateTime.UtcNow;
            var report = _scan.Execute(project.Name, dateTimeUtcNow);

            try
            {
                log.LogInformation($"Writing SecurityReport for project {project.Name} to Azure DevOps");
                await _client.AddCustomLogJsonAsync("SecurityScanReport", report, "Date");
                
                foreach (var applicationGroupPermissions in report.GlobalPermissions)
                {
                    await _client.AddCustomLogJsonAsync(
                        "SecurityScanReport",
                        new
                        {
                            ApplicationGroupPermissions = applicationGroupPermissions, 
                            isCompliant = applicationGroupPermissions.IsCompliant,
                            projectName = report.ProjectName,
                            Date = dateTimeUtcNow
                        }, 
                        "Date"
                        );
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Failed to write report to log analytics: {ex}");
                throw;
            }
        }
    }
}