using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SecurePipelineScan.Rules;
using SecurePipelineScan.VstsService;
using System;
using System.Collections.Generic;
using VstsLogAnalytics.Client;
using VstsLogAnalytics.Common;
using Requests = SecurePipelineScan.VstsService.Requests;

namespace VstsLogAnalyticsFunction
{
    public static class RepositoryScanFunction
    {
        [FunctionName("RepositoryScanFunction")]
        public static async System.Threading.Tasks.Task Run([TimerTrigger("0 */30 * * * *")] TimerInfo timerInfo,
            [Inject]ILogAnalyticsClient logAnalyticsClient,
            [Inject]IVstsRestClient client,
            ILogger log)
        {
            try
            {
                log.LogInformation($"Repository scan timed check start: {DateTime.Now}");

                var projects = client.Get(Requests.Project.Projects());

                log.LogInformation($"Projects found: {projects.Count}");
                List<Exception> aggregateExceptions = new List<Exception>();

                foreach (var p in projects.Value)
                {
                    try
                    {
                        var scan = new RepositoryScan(client);

                        var results = scan.Execute(p.Name);

                        foreach (var r in results)
                        {
                            var report = new
                            {
                                r.Project,
                                r.Repository,
                                r.HasRequiredReviewerPolicy,
                                Date = DateTime.UtcNow,
                            };
                            
                            await logAnalyticsClient.AddCustomLogJsonAsync("GitRepository", report, "Date");
                            log.LogInformation($"Project scanned: {r.Project}");
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