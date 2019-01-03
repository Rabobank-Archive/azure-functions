using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Rules.Reports;
using SecurePipelineScan.Rules.Checks;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;
using VstsLogAnalytics.Client;
using VstsLogAnalytics.Common;

//using SecurePipelineScan.VstsService.Response;

namespace VstsLogAnalyticsFunction
{
    public static class SecurityScanClientFunction
    {
        [FunctionName(nameof(SecurityScanClientFunction))]
        public static async Task Run([TimerTrigger("0 */5 * * * *", RunOnStartup = true)]
            TimerInfo timerInfo,
            [OrchestrationClient] DurableOrchestrationClientBase orchestrationClientBase,
            ILogger log)
        {
            const string functionName = nameof(SecurityScanOrchestrationFunction);
            var instanceId = await orchestrationClientBase.StartNewAsync(functionName, null);
            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
        }

        [FunctionName(nameof(SecurityScanOrchestrationFunction))]
        public static async Task<string> SecurityScanOrchestrationFunction(
            [OrchestrationTrigger] DurableOrchestrationContextBase context,
            [Inject] IVstsRestClient client,
            ILogger log
        )

        {
            var projects = client.Get(Project.Projects()).Value;

            log.LogInformation($"Creating tasks for every project");
            var tasks = projects.Select(x => context.CallActivityAsync<int>("CreateSecurityReportFunction", x));

            var enumerable = tasks.ToList();
            await Task.WhenAll(enumerable);

            return enumerable.Sum(t => t.Result).ToString();
        }

        [FunctionName(nameof(CreateSecurityReportFunction))]
        public static async Task CreateSecurityReportFunction(
            [ActivityTrigger] DurableActivityContext context,
            [Inject] ILogAnalyticsClient logAnalyticsClient,
            [Inject] IVstsRestClient client,
            ILogger log)
        {
            var project = context.GetInput<SecurePipelineScan.VstsService.Response.Project>();

            if (project == null) throw new ArgumentNullException("No project found from context while creating report");

            log.LogInformation($"Creating SecurityReport for project {project.Name}");

            var applicationGroups = client.Get(ApplicationGroup.ApplicationGroups(project.Id)).Identities;


            try
            {
                var report = new
                {
                    Project = project.Name,
                    ApplicationGroupContainsProductionEnvironmentOwner = ProjectApplicationGroup.ApplicationGroupContainsProductionEnvironmentOwner(applicationGroups),
                    Date = DateTime.UtcNow,
                };
                log.LogInformation($"Writing SecurityReport for project {project.Name} to Azure Devops");

                await logAnalyticsClient.AddCustomLogJsonAsync("SecurityScanReport", report, "Date");
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Failed to write report to log analytics: {ex}");
                throw;
            }
        }
    }
}