using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.TestManagement.WebApi;
using Moq;
using Newtonsoft.Json;
using RestSharp;
using Rules.Reports;
using SecurePipelineScan.Rules;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;
using VstsLogAnalytics.Client;
using Xunit;
using Response = SecurePipelineScan.VstsService.Response;

namespace VstsLogAnalyticsFunction.Tests
{
    public class SecurityScanFunctionTest
    {
        [Fact]
        public async Task ProductionOwnerResponseShouldBeSentToLogAnalytics()
        {
            var fixture = new Fixture();

            var logAnalyticsClient = new Mock<ILogAnalyticsClient>();
            var client = new Mock<IVstsRestClient>(MockBehavior.Strict);

            client.Setup(x => x.Get(It.IsAny<IVstsRestRequest<Response.Multiple<SecurePipelineScan.VstsService.Response.Project>>>()))
                .Returns(fixture.Create<Response.Multiple<SecurePipelineScan.VstsService.Response.Project>>());

            client.Setup(x => x.Get(It.IsAny<IVstsRestRequest<Response.ApplicationGroups>>())).Returns(
                fixture.Create<Response.ApplicationGroups>());

            await SecurityScanFunction.Run(new TimerInfo(null, null, false), logAnalyticsClient.Object, client.Object, new Mock<ILogger>().Object);

            logAnalyticsClient.Verify(x => x.AddCustomLogJsonAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>()), Times.AtLeastOnce());
        }

        [Fact]
        public async Task LogNameInAddCustomLogJsonAsyncShouldBeSecurityScanReport()
        {
            var fixture = new Fixture();

            var logAnalyticsClient = new Mock<ILogAnalyticsClient>();
            var client = new Mock<IVstsRestClient>(MockBehavior.Strict);

            client.Setup(x => x.Get(It.IsAny<IVstsRestRequest<Response.Multiple<SecurePipelineScan.VstsService.Response.Project>>>()))
                .Returns(fixture.Create<Response.Multiple<SecurePipelineScan.VstsService.Response.Project>>());

            client.Setup(x => x.Get(It.IsAny<IVstsRestRequest<Response.ApplicationGroups>>()))
                .Returns(fixture.Create<Response.ApplicationGroups>());

            await SecurityScanFunction.Run(new TimerInfo(null, null, false), logAnalyticsClient.Object, client.Object, new Mock<ILogger>().Object);

            logAnalyticsClient.Verify(x => x.AddCustomLogJsonAsync("SecurityScanReport", It.IsAny<object>(), It.IsAny<string>()),
                Times.AtLeastOnce());
        }

        [Fact]
        public async Task RunShouldGetAllAzDoProjects()
        {
            var fixture = new Fixture();

            var logAnalyticsClient = new Mock<ILogAnalyticsClient>();
            var client = new Mock<IVstsRestClient>(MockBehavior.Strict);

            client.Setup(x => x.Get(It.IsAny<IVstsRestRequest<Response.Multiple<Response.Project>>>()))
                .Returns(fixture.Create<Response.Multiple<Response.Project>>());

            client.Setup(x => x.Get(It.IsAny<IVstsRestRequest<Response.ApplicationGroups>>()))
                .Returns(fixture.Create<Response.ApplicationGroups>());

            await SecurityScanFunction.Run(new TimerInfo(null, null, false), logAnalyticsClient.Object, client.Object, new Mock<ILogger>().Object);

            client.Verify(x => x.Get(It.IsAny<IVstsRestRequest<Response.Multiple<Response.Project>>>()),
                Times.AtLeastOnce());
        }

       

    }
}