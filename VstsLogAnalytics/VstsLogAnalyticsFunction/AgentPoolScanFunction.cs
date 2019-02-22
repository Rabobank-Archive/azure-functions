using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using VstsLogAnalytics.Client;
using VstsLogAnalytics.Common;
using Requests = SecurePipelineScan.VstsService.Requests;

namespace VstsLogAnalyticsFunction
{
    public static class AgentPoolScanFunction
    {
        [FunctionName(nameof(AgentPoolScanFunction))]
        public static async System.Threading.Tasks.Task Run(
            [TimerTrigger("0 */5 * * * *", RunOnStartup =true)] TimerInfo timerInfo,
            [Inject]ILogAnalyticsClient logAnalyticsClient,
            [Inject] IVstsRestClient client,
            [Inject] HttpClient azureManagementClient,
            [Inject] IAadManager aadManager,
            ILogger log)
        {
            if (logAnalyticsClient == null) { throw new ArgumentNullException("Log Analytics Client is not set"); }
            if (client == null) { throw new ArgumentNullException("VSTS Rest client is not set"); }

            log.LogInformation("Time trigger function to check Azure DevOps agent status");

            List<AgentPoolInformation> observedPools = new List<AgentPoolInformation>();


            observedPools.Add(new AgentPoolInformation() { PoolName = "Rabo-Build-Azure-Linux", ResourceGroupPrefix = "rg-m01-prd-vstslinuxagents-0" });
            observedPools.Add(new AgentPoolInformation() { PoolName = "Rabo-Build-Azure-Linux-Canary", ResourceGroupPrefix = "rg-m01-prd-vstslinuxcanary-0" });
            observedPools.Add(new AgentPoolInformation() { PoolName = "Rabo-Build-Azure-Linux-Fallback", ResourceGroupPrefix = "rg-m01-prd-vstslinuxfallback-0" });
            observedPools.Add(new AgentPoolInformation() { PoolName = "Rabo-Build-Azure-Linux-Preview", ResourceGroupPrefix = "rg-m01-prd-vstslinuxpreview-0" });
            observedPools.Add(new AgentPoolInformation() { PoolName = "Rabo-Build-Azure-Windows", ResourceGroupPrefix = "rg-m01-prd-vstswinagents-0" });
            observedPools.Add(new AgentPoolInformation() { PoolName = "Rabo-Build-Azure-Windows-Canary", ResourceGroupPrefix = "rg-m01-prd-vstswincanary-0" });
            observedPools.Add(new AgentPoolInformation() { PoolName = "Rabo-Build-Azure-Windows-Fallback", ResourceGroupPrefix = "rg-m01-prd-vstswinfallback-0" });
            observedPools.Add(new AgentPoolInformation() { PoolName = "Rabo-Build-Azure-Windows-Preview", ResourceGroupPrefix = "rg-m01-prd-vstswinpreview-0" });


            var orgPools = client.Get(Requests.DistributedTask.OrganizationalAgentPools());

            var poolsToObserve = orgPools.Value.Where(x => observedPools.Any(p => p.PoolName == x.Name));

            List<LogAnalyticsAgentStatus> list = new List<LogAnalyticsAgentStatus>();

            foreach (var pool in poolsToObserve)
            {
                var poolStatus = client.Get(Requests.DistributedTask.AgentPoolStatus(pool.Id));

                foreach (var agent in poolStatus.Value)
                {
                    var assignedTask = (agent.Status != "online") ? "Offline" : ((agent.AssignedRequest == null) ? "Idle" : agent.AssignedRequest.PlanType);
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
                        Name = agent.Name,
                        Id = agent.Id,
                        Enabled = agent.Enabled,
                        Status = agent.Status,
                        StatusCode = statusCode,
                        Version = agent.Version,
                        AssignedTask = assignedTask,
                        Pool = pool.Name,
                        Date = DateTime.UtcNow,
                    });

                    if (assignedTask == "Offline")
                    {
                        var agentInfo = GetAgentInfoFromName(agent, pool, observedPools);

                        await ReImageAgent(log, agentInfo, azureManagementClient, aadManager);
                    }
                }
            }

            log.LogInformation("Done retrieving poolstatus information. Send to log analytics");
            await logAnalyticsClient.AddCustomLogJsonAsync("AgentStatus", list, "Date");
        }

        public static async System.Threading.Tasks.Task ReImageAgent(ILogger log, AgentInformation agentInfo, HttpClient client, IAadManager aadManager)
        {
            string accessToken = await aadManager.GetAccessTokenAsync().ConfigureAwait(false);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var agentStatusJson = await client.GetStringAsync($"https://management.azure.com/subscriptions/f13f81f8-7578-4ca8-83f3-0a845fad3cb5/resourceGroups/{agentInfo.ResourceGroup}/providers/Microsoft.Compute/virtualMachineScaleSets/agents/virtualmachines/{agentInfo.InstanceId}/instanceView?api-version=2018-06-01");
            dynamic status = JObject.Parse(agentStatusJson);
            if (status.statuses[0].code == "ProvisioningState/updating")
            {
                log.LogInformation($"Agent already being re-imaged: {agentInfo.ResourceGroup} - {agentInfo.InstanceId}");
                return;
            }

            log.LogInformation($"Re-image agent: {agentInfo.ResourceGroup} - {agentInfo.InstanceId}");
            var reimageResult = await client.PostAsync($"https://management.azure.com/subscriptions/f13f81f8-7578-4ca8-83f3-0a845fad3cb5/resourceGroups/{agentInfo.ResourceGroup}/providers/Microsoft.Compute/virtualMachineScaleSets/agents/virtualmachines/{agentInfo.InstanceId}/reimage?api-version=2018-06-01", new StringContent(""));
        }

        public static AgentInformation GetAgentInfoFromName(AgentStatus agent, AgentPoolInfo pool, IEnumerable<AgentPoolInformation> observedPools)
        {
            //re-image agent
            var rgPrefix = observedPools.FirstOrDefault(op => op.PoolName == pool.Name);
            var agentNameSplitted = agent.Name.Split('-');

            if (rgPrefix == null || agentNameSplitted.Length != 8)
            {
                throw new Exception($"Agent with illegal name detected. cannot re-image: {agent.Name}");
            }

            return new AgentInformation($"{rgPrefix.ResourceGroupPrefix}{agentNameSplitted[3]}", int.Parse(agentNameSplitted[6]));


        }
    }
}