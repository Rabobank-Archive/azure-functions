using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SecurePipelineScan.Rules;
using SecurePipelineScan.VstsService;
using System;
using System.Collections.Generic;
using Rules.Reports;
using VstsLogAnalytics.Client;
using VstsLogAnalytics.Common;
using Requests = SecurePipelineScan.VstsService.Requests;

namespace VstsLogAnalyticsFunction
{
    public static class RepositoryScanFunction
    {
        [FunctionName(nameof(RepositoryScanFunction))]
        public static async System.Threading.Tasks.Task Run(
            [TimerTrigger("0 */30 * * * *", RunOnStartup = true)] TimerInfo timerInfo,
            [Inject] ILogAnalyticsClient logAnalyticsClient,
            [Inject] IVstsRestClient client,
            [Inject] IProjectScan<IEnumerable<RepositoryReport>> scan,
            ILogger log)
        {
            if (logAnalyticsClient == null) { throw new ArgumentNullException("Log Analytics Client is not set"); }
            if (client == null) { throw new ArgumentNullException("VSTS Rest client is not set"); }
            if (scan == null) throw new ArgumentNullException(nameof(scan));

            try
            {
                log.LogInformation($"Repository scan timed check start: {DateTime.Now}");

                var projects = client.Get(Requests.Project.Projects());
                log.LogInformation($"Projects found: {projects.Count}");
                var aggregateExceptions = new List<Exception>();

                foreach (var p in projects.Value)
                {
                    try
                    {
                        var reports = scan.Execute(p.Name, timerInfo.ScheduleStatus.Last);
                        foreach (var report in reports)
                        {
                            await logAnalyticsClient.AddCustomLogJsonAsync("GitRepository", report, "Date");
                            log.LogInformation($"Project scanned: {report.Project}");
                        }
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
            catch (Exception ex)
            {
                log.LogError(ex, $"Failed to write repository scan to log analytics: {ex}");
                throw;
            }
        }
    }
}