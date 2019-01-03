using System;
using System.Collections.Generic;
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
    public class SecurityScanClientFunction
    {
        [FunctionName("SecurityScanClientFunction")]
        public static async Task StartSecurityClientScan([TimerTrigger("0 */1 * * * *", RunOnStartup = true)]
            TimerInfo timerInfo,
            [OrchestrationClient] DurableOrchestrationClientBase orchestrationClientBase,
            ILogger log)
        {
            const string functionName = "SecurityScanOrchestrationFunction";
            var instanceId = await orchestrationClientBase.StartNewAsync(functionName, null);
            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
        }

        [FunctionName("SecurityScanOrchestrationFunction")]
        public static async Task<string> SecurityScanOrchestration(
            [OrchestrationTrigger] DurableOrchestrationContextBase context,
            ILogger log
        )

        {
            var projects = await context.CallActivityAsync<string>("SecurityScanGetProjectsFunction", null);

            log.LogInformation($"Creating tasks for every project");
            var tasks = projects.Select(x => context.CallActivityAsync<int>("CreateSecurityReport", x));

            var enumerable = tasks.ToList();
            await Task.WhenAll(enumerable);

            return enumerable.Sum(t => t.Result).ToString();
        }

        [FunctionName("SecurityScanGetProjectsFunction")]
        public static IEnumerable<SecurePipelineScan.VstsService.Response.Project> SecurityScanGetProjects(
            [ActivityTrigger] 
            [Inject] IVstsRestClient client,
            ILogger log)
        {
            log.LogInformation($"Getting all Azure DevOps Projects");
            var projects = client.Get(Project.Projects()).Value;
            return projects;
        }

        [FunctionName("CreateSecurityReport")]
        public static async Task Run(
            [ActivityTrigger] DurableOrchestrationContext context,
            SecurePipelineScan.VstsService.Response.Project project,
            [Inject] ILogAnalyticsClient logAnalyticsClient,
            [Inject] IVstsRestClient client,
            ILogger log)
        {
            var applicationGroups = client.Get(ApplicationGroup.ApplicationGroups(project.Id)).Identities;


            log.LogInformation($"Creating SecurityReport for project {project.Name}");
            SecurityReport report = new SecurityReport
            {
                Project = project.Name,
                ApplicationGroupContainsProductionEnvironmentOwner = ProjectApplicationGroup.ApplicationGroupContainsProductionEnvironmentOwner(applicationGroups),
            };

            log.LogInformation($"Writing SecurityReport for project {project.Name} to Azure Devops");
            string date = DateTime.UtcNow.ToString();

            await logAnalyticsClient.AddCustomLogJsonAsync("SecurityScanReport", report, date);
        }
    }
}