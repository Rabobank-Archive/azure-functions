﻿using AutoFixture;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Moq;
using RestSharp;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
using System.Collections.Generic;
using VstsLogAnalytics.Client;
using Xunit;

namespace VstsLogAnalyticsFunction.Tests
{
    public class AgentPoolScanTests
    {
        [Fact]
        public void AgentPoolScanTest()
        {
            Fixture fixture = new Fixture();

            fixture.Register<Multiple<AgentPoolInfo>>(() => new Multiple<AgentPoolInfo>()
            {
                Value = new List<AgentPoolInfo>()
                {
                    new AgentPoolInfo { Name = "Rabo-Build-Azure-Linux", Id=1},
                    new AgentPoolInfo { Name = "Rabo-Build-Azure-Linux-Canary", Id=2},
                    new AgentPoolInfo { Name = "Rabo-Build-Azure-Linux-Fallback", Id=3},
                    new AgentPoolInfo { Name = "Rabo-Build-Azure-Linux-Preview", Id=4},
                    new AgentPoolInfo { Name = "Rabo-Build-Azure-Windows", Id=5},
                    new AgentPoolInfo { Name = "Rabo-Build-Azure-Windows-Canary", Id=6},
                    new AgentPoolInfo { Name = "Rabo-Build-Azure-Windows-Fallback", Id=7},
                    new AgentPoolInfo { Name = "Rabo-Build-Azure-Windows-Preview", Id=8 },
                    new AgentPoolInfo { Name = "Rabo-Build-Azure-Windows-NOT-OBSERVED", Id=9 },
                },
            });

            TimerInfo timerInfo = new TimerInfo(null, null, false);

            var logger = new Mock<ILogger>();
            var logAnalyticsClient = new Mock<ILogAnalyticsClient>();
            var vstsClient = new Mock<IVstsRestClient>();

            vstsClient.Setup(client => client.Execute(It.IsAny<IVstsRestRequest<Multiple<AgentPoolInfo>>>()))
                .Returns(new RestResponse<Multiple<AgentPoolInfo>> { Data = fixture.Create<Multiple<AgentPoolInfo>>() });

            vstsClient.Setup(client => client.Execute(It.IsAny<IVstsRestRequest<Multiple<AgentStatus>>>()))
                .Returns(new RestResponse<Multiple<AgentStatus>> { Data = fixture.Create<Multiple<AgentStatus>>() });

            AgentPoolScan.Run(timerInfo, logAnalyticsClient.Object, vstsClient.Object, logger.Object);

            vstsClient.Verify(v => v.Execute(It.IsAny<IVstsRestRequest<Multiple<AgentPoolInfo>>>()), Times.Exactly(1));

            vstsClient.Verify(v => v.Execute(It.IsAny<IVstsRestRequest<Multiple<AgentStatus>>>()), Times.Exactly(8));

            logAnalyticsClient.Verify(client => client.AddCustomLogJsonAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(1));
        }
    }
}