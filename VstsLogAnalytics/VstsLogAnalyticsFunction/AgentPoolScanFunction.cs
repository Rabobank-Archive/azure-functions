using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SecurePipelineScan.VstsService;
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
                        //re-image agent
                        var rgPrefix = observedPools.FirstOrDefault(op => op.PoolName == pool.Name);
                        var agentNameSplitted = agent.Name.Split('-');

                        if (rgPrefix == null || agentNameSplitted.Length != 7)
                        {
                            throw new Exception($"Agent with illegal name detected. cannot re-image: {agent.Name}");
                        }

                        var agentRegion = agentNameSplitted[3];
                        var agentVmId = agentNameSplitted[6];


                        var azureServiceTokenProvider2 = new AzureServiceTokenProvider();
                        string accessToken = await azureServiceTokenProvider2.GetAccessTokenAsync("https://management.azure.com/").ConfigureAwait(false);

                        HttpClient azureClient = new HttpClient();
                        azureClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                        var reimageResult = await azureClient.PostAsync($"https://management.azure.com/subscriptions/f13f81f8-7578-4ca8-83f3-0a845fad3cb5/resourceGroups/{rgPrefix}{agentRegion}/providers/Microsoft.Compute/virtualMachineScaleSets/agents/virtualmachines/{int.Parse(agentVmId)}/reimage?api-version=2018-06-01",new StringContent(""));
                        //var reimageResult = await azureClient.PostAsync($"https://management.azure.com/subscriptions/4562d10c-8487-4fcf-8bdf-5d9d729f5775/resourceGroups/vmss-test/providers/Microsoft.Compute/virtualMachineScaleSets/agents/virtualmachines/0/reimage?api-version=2018-06-01", new StringContent(""));
                    }
                }
            }

            log.LogInformation("Done retrieving poolstatus information. Send to log analytics");
            await logAnalyticsClient.AddCustomLogJsonAsync("AgentStatus", list, "Date");
        }
    }
}