using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SecurePipelineScan.Rules;
using SecurePipelineScan.Rules.Reports;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;
using System;
using System.Threading.Tasks;
using VstsLogAnalytics.Client;
using Project = SecurePipelineScan.VstsService.Response.Project;

namespace VstsLogAnalyticsFunction
{
    public class SecurityScanProjectActivity
    {
        private readonly ILogAnalyticsClient _client;
        private readonly IProjectScan<SecurityReport> _scan;
        private readonly IVstsRestClient _azuredo;
        private readonly IAzureDevOpsConfig _azuredoConfig;

        public SecurityScanProjectActivity(ILogAnalyticsClient client,
            IProjectScan<SecurityReport> scan,
            IVstsRestClient azuredo,
            IAzureDevOpsConfig azuredoConfig)
        {
            _client = client;
            _scan = scan;
            _azuredo = azuredo;
            _azuredoConfig = azuredoConfig;
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
                            ProjectName = report.ProjectName,
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

            try
            {
                var extensionData = report.Map();
                _azuredo.Put(ExtensionManagement.ExtensionData<ProjectOverviewData>("tas",
                    _azuredoConfig.ExtensionName,
              "ProjectOverview"), extensionData);
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Write Extension data failed: {ex}");
                throw;
            }
        }
    }
}