using AutoFixture;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Moq;
using RichardSzalay.MockHttp;
using SecurePipelineScan.VstsService;
using Response = SecurePipelineScan.VstsService.Response;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Services.AppAuthentication;
using Shouldly;
using Unmockable;
using LogAnalytics.Client;
using Xunit;

namespace Functions.Tests
{
    public class AgentPoolScanFunctionTests
    {

        [Fact]
        public async Task AgentPoolScanTest()
        {
            // Arrange
            var fixture = new Fixture();
            IEnumerable<Response.AgentPoolInfo> pools =  new[] { 
            new Response.AgentPoolInfo {Name = "Rabo-Build-Azure-Linux", Id = 1},
            new Response.AgentPoolInfo {Name = "Rabo-Build-Azure-Linux-Canary", Id = 2},
            new Response.AgentPoolInfo {Name = "Rabo-Build-Azure-Linux-Fallback", Id = 3},
            new Response.AgentPoolInfo {Name = "Rabo-Build-Azure-Linux-Preview", Id = 4},
            new Response.AgentPoolInfo {Name = "Rabo-Build-Azure-Windows", Id = 5},
            new Response.AgentPoolInfo {Name = "Rabo-Build-Azure-Windows-Canary", Id = 6},
            new Response.AgentPoolInfo {Name = "Rabo-Build-Azure-Windows-Fallback", Id = 7},
            new Response.AgentPoolInfo {Name = "Rabo-Build-Azure-Windows-Preview", Id = 8},
            new Response.AgentPoolInfo {Name = "Rabo-Build-Azure-Windows-NOT-OBSERVED", Id = 9}
        };

        fixture.Customize<Response.AgentStatus>(a => a.With(agent => agent.Status, "online"));

            var mockHttp = new MockHttpMessageHandler();

            mockHttp.When(HttpMethod.Get, "https://management.azure.com/subscriptions/f13f81f8-7578-4ca8-83f3-0a845fad3cb5/resourceGroups/*/providers/Microsoft.Compute/virtualMachineScaleSets/agents/virtualmachines/*/instanceView?api-version=2018-06-01")
                .Respond("application/json", "{ \"placementGroupId\": \"f79e82f0-3480-4eb3-a893-5cf9bd74daad\", \"platformUpdateDomain\": 0, \"platformFaultDomain\": 0, \"computerName\": \"agents2q3000000\", \"osName\": \"ubuntu\", \"osVersion\": \"18.04\", \"vmAgent\": { \"vmAgentVersion\": \"2.2.36\", \"statuses\": [ { \"code\": \"ProvisioningState/succeeded\", \"level\": \"Info\", \"displayStatus\": \"Ready\", \"message\": \"Guest Agent is running\", \"time\": \"2019-02-22T08:17:12+00:00\" } ], \"extensionHandlers\": [] }, \"disks\": [ { \"name\": \"agents_agents_0_OsDisk_1_d0e2afd2252041e98796d5ccdcf329d0\", \"statuses\": [ { \"code\": \"ProvisioningState/succeeded\", \"level\": \"Info\", \"displayStatus\": \"Provisioning succeeded\", \"time\": \"2019-02-22T08:16:59.1325957+00:00\" } ] }, { \"name\": \"agents_agents_0_OsDisk_1_3009fa8e43e144029be77cd72065f6df\", \"statuses\": [ { \"code\": \"ProvisioningState/deleting\", \"level\": \"Info\", \"displayStatus\": \"Deleting\" } ] } ], \"statuses\": [ { \"code\": \"ProvisioningState/updating\", \"level\": \"Info\", \"displayStatus\": \"Updating\" }, { \"code\": \"PowerState/running\", \"level\": \"Info\", \"displayStatus\": \"VM running\" } ] }");

            mockHttp.When(HttpMethod.Post, "https://management.azure.com/subscriptions/f13f81f8-7578-4ca8-83f3-0a845fad3cb5/resourceGroups/*/providers/Microsoft.Compute/virtualMachineScaleSets/agents/virtualmachines/*/reimage?api-version=2018-06-01")
                .Respond(HttpStatusCode.OK);

            var logAnalyticsClient = new Mock<ILogAnalyticsClient>();
            var tokenProvider = new Intercept<AzureServiceTokenProvider>();
            var client = new Mock<IVstsRestClient>();

            client.Setup(x => x.GetAsync(It.IsAny<IVstsRequest<Response.Multiple<Response.AgentPoolInfo>>>()))
                .Returns(Task.FromResult(pools));

            client.Setup(x => x.GetAsync(It.IsAny<IVstsRequest<Response.Multiple<Response.AgentStatus>>>()))
                .Returns(Task.FromResult(fixture.CreateMany<Response.AgentStatus>()));


            // Act
            var function = new AgentPoolScanFunction(logAnalyticsClient.Object, client.Object, mockHttp.ToHttpClient(), tokenProvider);
            await function.Run(new TimerInfo(null, null), new Mock<ILogger>().Object);

            // Assert
            client
                .Verify(v => v.GetAsync(It.IsAny<IVstsRequest<Response.Multiple<Response.AgentPoolInfo>>>()), 
                    Times.Exactly(1));

            client
                .Verify(v => v.GetAsync(It.IsAny<IVstsRequest<Response.Multiple<Response.AgentStatus>>>()), 
                    Times.Exactly(8));

            logAnalyticsClient
                .Verify(x => x.AddCustomLogJsonAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>()), 
                    Times.Exactly(1));
        }

        [Fact]
        public void GetAgentInfoFromNameShouldSplitNamesIntoRightValues()
        {
            //Arrange
            var observedPools = new List<AgentPoolInformation>();
            observedPools.Add(new AgentPoolInformation() { PoolName = "Rabo-Build-Azure-Linux", ResourceGroupPrefix = "rg-m01-prd-vstslinuxagents-0" });
            observedPools.Add(new AgentPoolInformation() { PoolName = "Rabo-Build-Azure-Linux-Canary", ResourceGroupPrefix = "rg-m01-prd-vstslinuxcanary-0" });
            observedPools.Add(new AgentPoolInformation() { PoolName = "Rabo-Build-Azure-Linux-Fallback", ResourceGroupPrefix = "rg-m01-prd-vstslinuxfallback-0" });
            observedPools.Add(new AgentPoolInformation() { PoolName = "Rabo-Build-Azure-Linux-Preview", ResourceGroupPrefix = "rg-m01-prd-vstslinuxpreview-0" });
            observedPools.Add(new AgentPoolInformation() { PoolName = "Rabo-Build-Azure-Windows", ResourceGroupPrefix = "rg-m01-prd-vstswinagents-0" });
            observedPools.Add(new AgentPoolInformation() { PoolName = "Rabo-Build-Azure-Windows-Canary", ResourceGroupPrefix = "rg-m01-prd-vstswincanary-0" });
            observedPools.Add(new AgentPoolInformation() { PoolName = "Rabo-Build-Azure-Windows-Fallback", ResourceGroupPrefix = "rg-m01-prd-vstswinfallback-0" });
            observedPools.Add(new AgentPoolInformation() { PoolName = "Rabo-Build-Azure-Windows-Preview", ResourceGroupPrefix = "rg-m01-prd-vstswinpreview-0" });

            var fixture = new Fixture();
            fixture.Customize<Response.AgentStatus>(a => a.With(agent => agent.Name, "linux-agent-canary-1-84432-2296-000000-1"));

            fixture.Customize<Response.AgentPoolInfo>(a => a.With(pool => pool.Name, "Rabo-Build-Azure-Linux-Canary"));

            var result = AgentPoolScanFunction.GetAgentInfoFromName(fixture.Create<Response.AgentStatus>(),
                                                       fixture.Create<Response.AgentPoolInfo>(),
                                                       observedPools);
            Assert.Equal(0, result.InstanceId);
            Assert.Equal("rg-m01-prd-vstslinuxcanary-01", result.ResourceGroup);

        }

        [Fact]
        public async Task AgentStillReImagingShouldNotReImageAgain()
        {
            //Arrange
            var log = new Mock<ILogger>();

            var mockHttp = new MockHttpMessageHandler();

            var getRequest = mockHttp.When(HttpMethod.Get, "https://management.azure.com/subscriptions/f13f81f8-7578-4ca8-83f3-0a845fad3cb5/resourceGroups/*/providers/Microsoft.Compute/virtualMachineScaleSets/agents/virtualmachines/*/instanceView?api-version=2018-06-01")
                    .Respond("application/json", "{ \"placementGroupId\": \"f79e82f0-3480-4eb3-a893-5cf9bd74daad\", \"platformUpdateDomain\": 0, \"platformFaultDomain\": 0, \"computerName\": \"agents2q3000000\", \"osName\": \"ubuntu\", \"osVersion\": \"18.04\", \"vmAgent\": { \"vmAgentVersion\": \"2.2.36\", \"statuses\": [ { \"code\": \"ProvisioningState/succeeded\", \"level\": \"Info\", \"displayStatus\": \"Ready\", \"message\": \"Guest Agent is running\", \"time\": \"2019-02-22T08:17:12+00:00\" } ], \"extensionHandlers\": [] }, \"disks\": [ { \"name\": \"agents_agents_0_OsDisk_1_d0e2afd2252041e98796d5ccdcf329d0\", \"statuses\": [ { \"code\": \"ProvisioningState/succeeded\", \"level\": \"Info\", \"displayStatus\": \"Provisioning succeeded\", \"time\": \"2019-02-22T08:16:59.1325957+00:00\" } ] }, { \"name\": \"agents_agents_0_OsDisk_1_3009fa8e43e144029be77cd72065f6df\", \"statuses\": [ { \"code\": \"ProvisioningState/deleting\", \"level\": \"Info\", \"displayStatus\": \"Deleting\" } ] } ], \"statuses\": [ { \"code\": \"ProvisioningState/updating\", \"level\": \"Info\", \"displayStatus\": \"Updating\" }, { \"code\": \"PowerState/running\", \"level\": \"Info\", \"displayStatus\": \"VM running\" } ] }");

            var postRequest = mockHttp.When(HttpMethod.Post, "https://management.azure.com/subscriptions/f13f81f8-7578-4ca8-83f3-0a845fad3cb5/resourceGroups/*/providers/Microsoft.Compute/virtualMachineScaleSets/agents/virtualmachines/*/reimage?api-version=2018-06-01")
                   .Respond(HttpStatusCode.OK);

            var agentInfo = new AgentInformation("rg", 0);

            var tokenProvider = new Intercept<AzureServiceTokenProvider>();
            tokenProvider
                .Setup(x => x.GetAccessTokenAsync(Arg.Ignore<string>(), null))
                .Returns(string.Empty);

            //Act
            await AgentPoolScanFunction.ReImageAgent(log.Object, agentInfo, mockHttp.ToHttpClient(), tokenProvider);

            //Assert

            //check if queried status
            mockHttp.GetMatchCount(getRequest).ShouldBe(1);

            //check if reimage called
            mockHttp.GetMatchCount(postRequest).ShouldBe(0);
        }

        [Fact]
        public async Task AgentOfflineShouldCheckStatusAndReImage()
        {

            //Arrange
            var log = new Mock<ILogger>();

            var mockHttp = new MockHttpMessageHandler();
            var getRequest = mockHttp.When(HttpMethod.Get, "https://management.azure.com/subscriptions/f13f81f8-7578-4ca8-83f3-0a845fad3cb5/resourceGroups/*/providers/Microsoft.Compute/virtualMachineScaleSets/agents/virtualmachines/*/instanceView?api-version=2018-06-01")
                    .Respond("application/json", "{ \"placementGroupId\": \"f79e82f0-3480-4eb3-a893-5cf9bd74daad\", \"platformUpdateDomain\": 0, \"platformFaultDomain\": 0, \"computerName\": \"agents2q3000000\", \"osName\": \"ubuntu\", \"osVersion\": \"18.04\", \"vmAgent\": { \"vmAgentVersion\": \"2.2.36\", \"statuses\": [ { \"code\": \"ProvisioningState/succeeded\", \"level\": \"Info\", \"displayStatus\": \"Ready\", \"message\": \"Guest Agent is running\", \"time\": \"2019-02-22T08:15:48+00:00\" } ], \"extensionHandlers\": [] }, \"disks\": [ { \"name\": \"agents_agents_0_OsDisk_1_3009fa8e43e144029be77cd72065f6df\", \"statuses\": [ { \"code\": \"ProvisioningState/succeeded\", \"level\": \"Info\", \"displayStatus\": \"Provisioning succeeded\", \"time\": \"2019-02-06T11:45:35.5975265+00:00\" } ] } ], \"statuses\": [ { \"code\": \"ProvisioningState/succeeded\", \"level\": \"Info\", \"displayStatus\": \"Provisioning succeeded\", \"time\": \"2019-02-06T11:46:58.0511995+00:00\" }, { \"code\": \"PowerState/running\", \"level\": \"Info\", \"displayStatus\": \"VM running\" } ] } ");

            var postRequest = mockHttp.When(HttpMethod.Post, "https://management.azure.com/subscriptions/f13f81f8-7578-4ca8-83f3-0a845fad3cb5/resourceGroups/*/providers/Microsoft.Compute/virtualMachineScaleSets/agents/virtualmachines/*/reimage?api-version=2018-06-01")
                   .Respond(HttpStatusCode.OK);

            var agentInfo = new AgentInformation("rg", 0);


            var tokenProvider = new Intercept<AzureServiceTokenProvider>();
            tokenProvider
                .Setup(x => x.GetAccessTokenAsync(Arg.Ignore<string>(), null))
                .Returns(string.Empty);

            //Act
            await AgentPoolScanFunction.ReImageAgent(log.Object, agentInfo, mockHttp.ToHttpClient(), tokenProvider);

            //Assert

            //check if queried status
            mockHttp.GetMatchCount(getRequest).ShouldBe(1);

            //check if reimage called
            mockHttp.GetMatchCount(postRequest).ShouldBe(1);
        }
    }
}