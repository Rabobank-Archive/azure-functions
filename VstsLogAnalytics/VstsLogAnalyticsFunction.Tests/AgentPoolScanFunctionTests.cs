using AutoFixture;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Moq;
using RestSharp;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
using System.Collections.Generic;
using System.Linq;
using VstsLogAnalytics.Client;
using Xunit;

namespace VstsLogAnalyticsFunction.Tests
{
    public class AgentPoolScanFunctionTests
    {
        [Fact]
        public async System.Threading.Tasks.Task AgentPoolScanTest()
        {
            // Arrange
            var fixture = new Fixture();
            fixture.Register(() => new Multiple<AgentPoolInfo>(
                new AgentPoolInfo { Name = "Rabo-Build-Azure-Linux", Id=1},
                new AgentPoolInfo { Name = "Rabo-Build-Azure-Linux-Canary", Id=2},
                new AgentPoolInfo { Name = "Rabo-Build-Azure-Linux-Fallback", Id=3},
                new AgentPoolInfo { Name = "Rabo-Build-Azure-Linux-Preview", Id=4},
                new AgentPoolInfo { Name = "Rabo-Build-Azure-Windows", Id=5},
                new AgentPoolInfo { Name = "Rabo-Build-Azure-Windows-Canary", Id=6},
                new AgentPoolInfo { Name = "Rabo-Build-Azure-Windows-Fallback", Id=7},
                new AgentPoolInfo { Name = "Rabo-Build-Azure-Windows-Preview", Id=8 },
                new AgentPoolInfo { Name = "Rabo-Build-Azure-Windows-NOT-OBSERVED", Id=9}
            ));

            fixture.Customize<AgentStatus>(a => a.With(agent => agent.Status, "online"));

            var logAnalyticsClient = new Mock<ILogAnalyticsClient>();
            var client = new Mock<IVstsRestClient>();

            client.Setup(x => x.Get(It.IsAny<IVstsRestRequest<Multiple<AgentPoolInfo>>>()))
                .Returns(fixture.Create<Multiple<AgentPoolInfo>>());

            client.Setup(x => x.Get(It.IsAny<IVstsRestRequest<Multiple<AgentStatus>>>()))
                .Returns(new Multiple<AgentStatus>(fixture.CreateMany<AgentStatus>().ToArray()));

            // Act
            await AgentPoolScanFunction.Run(
                new TimerInfo(null, null), 
                logAnalyticsClient.Object, 
                client.Object, 
                new Mock<ILogger>().Object);

            // Assert
            client
                .Verify(v => v.Get(It.IsAny<IVstsRestRequest<Multiple<AgentPoolInfo>>>()), 
                    Times.Exactly(1));

            client
                .Verify(v => v.Get(It.IsAny<IVstsRestRequest<Multiple<AgentStatus>>>()), 
                    Times.Exactly(8));

            logAnalyticsClient
                .Verify(x => x.AddCustomLogJsonAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>()), 
                    Times.Exactly(1));
        }

        [Fact]
        public void GetAgentInfoFromNameShouldSplitNamesIntoRightValues()
        {
            //Arrange
            List<AgentPoolInformation> observedPools = new List<AgentPoolInformation>();
            observedPools.Add(new AgentPoolInformation() { PoolName = "Rabo-Build-Azure-Linux", ResourceGroupPrefix = "rg-m01-prd-vstslinuxagents-0" });
            observedPools.Add(new AgentPoolInformation() { PoolName = "Rabo-Build-Azure-Linux-Canary", ResourceGroupPrefix = "rg-m01-prd-vstslinuxcanary-0" });
            observedPools.Add(new AgentPoolInformation() { PoolName = "Rabo-Build-Azure-Linux-Fallback", ResourceGroupPrefix = "rg-m01-prd-vstslinuxfallback-0" });
            observedPools.Add(new AgentPoolInformation() { PoolName = "Rabo-Build-Azure-Linux-Preview", ResourceGroupPrefix = "rg-m01-prd-vstslinuxpreview-0" });
            observedPools.Add(new AgentPoolInformation() { PoolName = "Rabo-Build-Azure-Windows", ResourceGroupPrefix = "rg-m01-prd-vstswinagents-0" });
            observedPools.Add(new AgentPoolInformation() { PoolName = "Rabo-Build-Azure-Windows-Canary", ResourceGroupPrefix = "rg-m01-prd-vstswincanary-0" });
            observedPools.Add(new AgentPoolInformation() { PoolName = "Rabo-Build-Azure-Windows-Fallback", ResourceGroupPrefix = "rg-m01-prd-vstswinfallback-0" });
            observedPools.Add(new AgentPoolInformation() { PoolName = "Rabo-Build-Azure-Windows-Preview", ResourceGroupPrefix = "rg-m01-prd-vstswinpreview-0" });

            var fixture = new Fixture();
            fixture.Customize<AgentStatus>(a => a.With(agent => agent.Name, "linux-agent-canary-1-84432-2296-000000-1"));

            fixture.Customize<AgentPoolInfo>(a => a.With(pool => pool.Name, "Rabo-Build-Azure-Linux-Canary"));

            var result = AgentPoolScanFunction.GetAgentInfoFromName(fixture.Create<AgentStatus>(),
                                                       fixture.Create<AgentPoolInfo>(),
                                                       observedPools);
            Assert.Equal(0, result.InstanceId);
            Assert.Equal("rg-m01-prd-vstslinuxcanary-01", result.ResourceGroup);

        }
    }
}