using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SecurePipelineScan.VstsService;
using System;
using System.Collections.Generic;
using System.Linq;
using VstsLogAnalytics.Client;
using VstsLogAnalytics.Common;
using Requests = SecurePipelineScan.VstsService.Requests;

namespace VstsLogAnalyticsFunction
{
    public static class AgentPoolScanFunction
    {
        [FunctionName(nameof(AgentPoolScanFunction))]
        public static async System.Threading.Tasks.Task Run(
            [TimerTrigger("0 */5 * * * *", RunOnStartup = true)] TimerInfo timerInfo,
            [Inject]ILogAnalyticsClient logAnalyticsClient,
            [Inject] IVstsRestClient client,
            ILogger log)
        {
            if (logAnalyticsClient == null) { throw new ArgumentNullException("Log Analytics Client is not set"); }
            if (client == null) { throw new ArgumentNullException("VSTS Rest client is not set"); }

            log.LogInformation("Time trigger function to check Azure DevOps agent status");

            string[] observedPools =
            {
                "Rabo-Build-Azure-Linux",
                "Rabo-Build-Azure-Linux-Canary" ,
                "Rabo-Build-Azure-Linux-Fallback" ,
                "Rabo-Build-Azure-Linux-Preview" ,
                "Rabo-Build-Azure-Windows" ,
                "Rabo-Build-Azure-Windows-Canary" ,
                "Rabo-Build-Azure-Windows-Fallback",
                "Rabo-Build-Azure-Windows-Preview"
            };

            var orgPools = client.Get(Requests.DistributedTask.OrganizationalAgentPools());

            var agentsToObserve = orgPools.Value.Where(x => observedPools.Contains(x.Name));

            List<LogAnalyticsAgentStatus> list = new List<LogAnalyticsAgentStatus>();

            foreach (var a in agentsToObserve)
            {
                var poolStatus = client.Get(Requests.DistributedTask.AgentPoolStatus(a.Id));

                foreach (var p in poolStatus.Value)
                {
                    var assignedTask = (p.Status != "online") ? "Offline" : ((p.AssignedRequest == null) ? "Idle" : p.AssignedRequest.PlanType);
                    int statusCode = 0;
                    switch (assignedTask)
                    {
                        case "Idle": statusCode = 1; break;
                        case "Build": statusCode = 2; break;
                        case "Release": statusCode = 3; break;
                        case "Offline":
                        default: statusCode = 0; break;
                    }

                    list.Add(new LogAnalyticsAgentStatus
                    {
                        Name = p.Name,
                        Id = p.Id,
                        Enabled = p.Enabled,
                        Status = p.Status,
                        StatusCode = statusCode,
                        Version = p.Version,
                        AssignedTask = assignedTask,
                        Pool = a.Name,
                        Date = DateTime.UtcNow,
                    });
                }
            }

            log.LogInformation("Done retrieving poolstatus information. Send to log analytics");

            await logAnalyticsClient.AddCustomLogJsonAsync("AgentStatus", list, "Date");
        }
    }
}