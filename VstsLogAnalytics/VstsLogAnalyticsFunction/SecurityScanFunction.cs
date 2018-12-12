using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SecurePipelineScan.Rules;
using SecurePipelineScan.VstsService;
using VstsLogAnalytics.Client;
using VstsLogAnalytics.Common;

namespace VstsLogAnalyticsFunction
{
    public static class SecurityScanFunction
    {
        [FunctionName("SecurityScanFunction")]
        public static async Task Run([TimerTrigger("0 */30 * * * *")] TimerInfo timerInfo, 
            [Inject]ILogAnalyticsClient logAnalyticsClient,
            [Inject]IVstsRestClient client, 
            ILogger log)
        {
            try
            {
                log.LogInformation($"Security scan timed check start: {DateTime.Now}");

                var response = client.Execute(SecurePipelineScan.VstsService.Requests.Project.Projects());
                var projects = response.Data;
                

                log.LogInformation($"Projects found: {projects.Count}");

                var securityReportScan = new SecurityReportScan(client);

                foreach (var project in projects.Value)
                {
                    var securityReport = securityReportScan.Execute(project.Name);
                    var securityCustomLog = new SecurityCustomLog
                    {
                        hasProductionEnvOwner = securityReport.ApplicationGroupContainsProductionEnvironmentOwner,
                        ProjectName = project.Name,
                    };

                    await logAnalyticsClient.AddCustomLogJsonAsync("SecurityScanReport",
                        JsonConvert.SerializeObject(new
                        {
                            securityCustomLog.ProjectName,
                            securityCustomLog.hasProductionEnvOwner,
                            Date = DateTime.UtcNow,
                        }), "Date");

                    log.LogInformation($"Project scanned: {securityCustomLog.ProjectName}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }
    }
}