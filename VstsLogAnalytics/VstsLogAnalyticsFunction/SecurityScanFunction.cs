using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Schema;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SecurePipelineScan.Rules;
using SecurePipelineScan.VstsService;
using VstsLogAnalytics.Client;
using VstsLogAnalytics.Common;
using Requests = SecurePipelineScan.VstsService.Requests;

namespace VstsLogAnalyticsFunction
{
    public static class SecurityScanFunction
    {
        [Disable]
        [FunctionName(nameof(SecurityScanFunction))]
        public static async Task Run(
            [TimerTrigger("0 */30 * * * *", RunOnStartup = true)]
            TimerInfo timerInfo,
            [Inject] ILogAnalyticsClient logAnalyticsClient,
            [Inject] IVstsRestClient client,
            ILogger log)
        {
            if (logAnalyticsClient == null)
            {
                throw new ArgumentNullException("Log Analytics Client is not set");
            }

            if (client == null)
            {
                throw new ArgumentNullException("VSTS Rest client is not set");
            }

            try
            {
                log.LogInformation($"Security scan timed check start: {DateTime.UtcNow}");

                var projects = client.Get(Requests.Project.Projects());

                log.LogInformation($"Projects found: {projects.Count}");
                List<Exception> aggregateExceptions = new List<Exception>();
                int currentNumber = 1;

                foreach (var project in projects.Value)
                {
                    try
                    {
                        log.LogInformation($"Scan {currentNumber} of total {projects.Count}");
                        log.LogInformation($"{DateTime.UtcNow} Start scan for project : {project.Name}");
                        var securityReportScan = new SecurityReportScan(client);
                        var securityReport = securityReportScan.Execute(project.Name);
                        var report = new
                        {
                            securityReport.Project,
                            securityReport.ProjectIsSecure,
                            securityReport.ApplicationGroupContainsProductionEnvironmentOwner,
                            securityReport.ProjectAdminGroupOnlyContainsRabobankProjectAdminGroup,
                            Date = DateTime.UtcNow,
                        };

                        await logAnalyticsClient.AddCustomLogJsonAsync("SecurityScanReport", report, "Date");
                        log.LogInformation($"{project.Name} : IsSecure {securityReport.ProjectIsSecure}");
                        log.LogInformation($"{project.Name} : ProductEnvOwner {securityReport.ApplicationGroupContainsProductionEnvironmentOwner}");
                        log.LogInformation($"{project.Name} : RaboProjectAdmin {securityReport.ProjectAdminGroupOnlyContainsRabobankProjectAdminGroup}");
                        log.LogInformation($"{DateTime.UtcNow} Finished scan for project : {project.Name}");
                    }
                    catch (Exception e)
                    {
                        aggregateExceptions.Add(e);
                    }

                    ++currentNumber;
                }

                if (aggregateExceptions.Count > 0)
                {
                    throw new AggregateException(aggregateExceptions);
                }
            }
            catch (Exception exception)
            {
                log.LogError(exception, $"Failed to write security scan to log analytics : {exception}");
                throw;
            }
        }
    }
}