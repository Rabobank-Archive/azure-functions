using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Rules.Reports;
using SecurePipelineScan.Rules;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
using VstsLogAnalytics.Client;
using VstsLogAnalytics.Common;
using Requests = SecurePipelineScan.VstsService.Requests;

namespace VstsLogAnalyticsFunction
{
    public static class SecurityScanFunction
    {
        [FunctionName("SecurityScanFunction")]
         public static async Task Run([TimerTrigger("0 */30 * * * *", RunOnStartup = true)] TimerInfo timerInfo,
            [Inject] ILogAnalyticsClient logAnalyticsClient,
            [Inject] IVstsRestClient client,
            ILogger log)
        {
            try
            {
                log.LogInformation($"Security scan timed check start: {DateTime.Now}");

                var projects = client.Get(Requests.Project.Projects());
                
                log.LogInformation($"Projects found: {projects.Count}");
                List<Exception> aggregateExceptions = new List<Exception>();


                foreach (var project in projects.Value)
                {
                    try
                    {
                        var securityReportScan = new SecurityReportScan(client);
                        var securityReport = securityReportScan.Execute(project.Name);
                        var report = new
                        {
                            project.Name,
                            securityReport.ApplicationGroupContainsProductionEnvironmentOwner,
                            Date = DateTime.UtcNow,
                        };
                        
                        await logAnalyticsClient.AddCustomLogJsonAsync("SecurityScanReport", report, "Date");
                        log.LogInformation($"Project scanned: {project.Name}");
                    }
                    catch (Exception e)
                    {
                        aggregateExceptions.Add(e);
                        
                    }
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