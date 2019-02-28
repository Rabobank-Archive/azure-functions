using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SecurePipelineScan.Rules;
using Requests = SecurePipelineScan.VstsService.Requests;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
using VstsLogAnalytics.Client;
using VstsLogAnalytics.Common;
using System.Linq;
using SecurePipelineScan.Rules.Reports;

namespace VstsLogAnalyticsFunction
{
    public class RepositoryScanProjectActivity
    {

        private readonly ILogAnalyticsClient _client;
        private readonly IProjectScan<IEnumerable<RepositoryReport>> _scan;
        private readonly IVstsRestClient _azuredo;

        public RepositoryScanProjectActivity(ILogAnalyticsClient client,
            IProjectScan<IEnumerable<RepositoryReport>> scan,
            IVstsRestClient azuredo)
        {
            _client = client;
            _scan = scan;
            _azuredo = azuredo;
        }

        [FunctionName(nameof(RepositoryScanProjectActivity))]
        public async Task<IEnumerable<RepositoryReport>> Run(
            [ActivityTrigger] DurableActivityContextBase context,
            ILogger log)
        {

            var reports = new List<RepositoryReport>();
            var aggregateExceptions = new List<Exception>();

            var project = context.GetInput<Project>();
            try
            {
                var projectreports = _scan.Execute(project.Name, DateTime.Now).ToList();
                foreach (var report in projectreports)
                {
                    try
                    {
                        await _client.AddCustomLogJsonAsync("GitRepository", report, "Date");
                        log.LogInformation($"Project: {report.Project}, repo: {report.Repository}");
                        reports.Add(report);
                    }
                    catch (Exception e)
                    {
                        aggregateExceptions.Add(e);
                    }
                }

                _azuredo.Put(
                    Requests.ExtensionManagement.ExtensionData<ExtensionDataReports<RepositoryReport>>("tas", "tas",
                        "GitRepositories"), new ExtensionDataReports<RepositoryReport> { Reports = projectreports, Id = project.Name });

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