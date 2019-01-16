using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Rules.Reports;
using SecurePipelineScan.Rules;
using SecurePipelineScan.Rules.Checks;
using SecurePipelineScan.VstsService.Response;
using VstsLogAnalytics.Client;
using VstsLogAnalytics.Common;

namespace VstsLogAnalyticsFunction.RepositoryScan
{
    public static class RepositoryScanProjectActivity
    {
        [FunctionName(nameof(RepositoryScanProjectActivity))]
        public static async Task<IEnumerable<RepositoryReport>> Run(
            [ActivityTrigger] DurableActivityContextBase context,
            [OrchestrationClient] DurableOrchestrationClientBase starter,
            [Inject] ILogAnalyticsClient logAnalyticsClient,
            [Inject] IProjectScan<IEnumerable<RepositoryReport>> scan,
            ILogger log)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (logAnalyticsClient == null) throw new ArgumentNullException(nameof(logAnalyticsClient));
            if (scan == null) throw new ArgumentNullException(nameof(scan));

            List<RepositoryReport> reports = new List<RepositoryReport>();
            var aggregateExceptions = new List<Exception>();

            var project = context.GetInput<Project>();
            try
            {
                var projectreports = scan.Execute(project.Name,DateTime.Now);
                foreach (var report in projectreports)
                {
                    try
                    {
                        await logAnalyticsClient.AddCustomLogJsonAsync("GitRepository", report, "Date");
                        log.LogInformation($"Project scanned: {report.Project}");
                        reports.Add(report);
                    }
                    catch (Exception e)
                    {
                        aggregateExceptions.Add(e);
                    }
                }
            }
            catch (Exception e)
            {
                aggregateExceptions.Add(e);
            }

            if (aggregateExceptions.Count > 0)
            {
                throw new AggregateException(aggregateExceptions);
            }

            return reports;
        }
    }
}