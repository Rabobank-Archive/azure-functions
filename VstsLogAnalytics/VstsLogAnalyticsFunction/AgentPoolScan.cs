using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SecurePipelineScan.VstsService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VstsLogAnalytics.Client;
using VstsLogAnalytics.Common;

namespace VstsLogAnalyticsFunction
{
    public static class AgentPoolScan
    {
        [FunctionName("AgentPoolScan")]
        public static async Task Run(
            [TimerTrigger("0 */5 * * * *")] TimerInfo timerInfo,
            [Inject]ILogAnalyticsClient logAnalyticsClient,
            [Inject] IVstsRestClient client,
            ILogger log)
        {
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

            var orgPools = client.Execute(Requests.DistributedTask.OrganizationalAgentPools());

            var agentsToObserve = orgPools.Data.Value.Where(x => observedPools.Contains(x.Name));

            List<LogAnalyticsAgentStatus> list = new List<LogAnalyticsAgentStatus>();

            foreach (var a in agentsToObserve)
            {
                var poolStatus = client.Execute(Requests.DistributedTask.AgentPoolStatus(a.Id));

                foreach (var p in poolStatus.Data.Value)
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

            await logAnalyticsClient.AddCustomLogJsonAsync("AgentStatus",
                JsonConvert.SerializeObject(list), "Date");
        }
    }
}