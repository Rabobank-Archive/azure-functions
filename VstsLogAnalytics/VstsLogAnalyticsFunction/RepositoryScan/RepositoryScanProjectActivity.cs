using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Rules.Reports;
using SecurePipelineScan.Rules;
using Requests = SecurePipelineScan.VstsService.Requests;
using SecurePipelineScan.VstsService;
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
            [Inject] ILogAnalyticsClient analytics,
            [Inject] IProjectScan<IEnumerable<RepositoryReport>> scan,
            [Inject] IVstsRestClient azure,
            ILogger log)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (analytics == null) throw new ArgumentNullException(nameof(analytics));
            if (scan == null) throw new ArgumentNullException(nameof(scan));

            var reports = new List<RepositoryReport>();
            var aggregateExceptions = new List<Exception>();

            var project = context.GetInput<Project>();
            try
            {
                var projectreports = scan.Execute(project.Name,DateTime.Now);
                foreach (var report in projectreports)
                {
                    try
                    {
                        await analytics.AddCustomLogJsonAsync("GitRepository", report, "Date");
                        log.LogInformation($"Project scanned: {report.Project}");
                        reports.Add(report);
                    }
                    catch (Exception e)
                    {
                        aggregateExceptions.Add(e);
                    }
                }
                
                azure.Put(
                    Requests.ExtensionManagement.ExtensionData<ExtensionDataReports>("tas", "tas",
                        "GitRepositories"), new ExtensionDataReports { Reports = projectreports });

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