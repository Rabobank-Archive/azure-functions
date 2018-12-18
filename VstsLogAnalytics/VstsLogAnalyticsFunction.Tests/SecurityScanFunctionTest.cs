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
        public async Task productionOwnerResponseShouldBeSentToLogAnalytics()
        {
            Fixture fixture = new Fixture();

            var logAnalyticsClient = new Mock<ILogAnalyticsClient>();
            var vstsClient = new Mock<IVstsRestClient>(MockBehavior.Strict);

            vstsClient.Setup(client => client.Get(It.IsAny<IVstsRestRequest<Response.Multiple<SecurePipelineScan.VstsService.Response.Project>>>()))
                .Returns(fixture.Create<Response.Multiple<SecurePipelineScan.VstsService.Response.Project>>());

            vstsClient.Setup(client => client.Get(It.IsAny<IVstsRestRequest<Response.ApplicationGroups>>())).Returns(
                fixture.Create<Response.ApplicationGroups>());

            await SecurityScanFunction.Run(new TimerInfo(null, null, false), logAnalyticsClient.Object, vstsClient.Object, new Mock<ILogger>().Object);

            logAnalyticsClient.Verify(client => client.AddCustomLogJsonAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce());
        }

        [Fact]
        public async Task logNameInAddCustomLogJsonAsyncShouldBeSecurityScanReport()
        {
            Fixture fixture = new Fixture();

            var logAnalyticsClient = new Mock<ILogAnalyticsClient>();
            var vstsClient = new Mock<IVstsRestClient>(MockBehavior.Strict);

            vstsClient.Setup(client => client.Get(It.IsAny<IVstsRestRequest<Response.Multiple<SecurePipelineScan.VstsService.Response.Project>>>()))
                .Returns(fixture.Create<Response.Multiple<SecurePipelineScan.VstsService.Response.Project>>());

            vstsClient.Setup(client => client.Get(It.IsAny<IVstsRestRequest<Response.ApplicationGroups>>()))
                .Returns(fixture.Create<Response.ApplicationGroups>());

            await SecurityScanFunction.Run(new TimerInfo(null, null, false), logAnalyticsClient.Object, vstsClient.Object, new Mock<ILogger>().Object);

            logAnalyticsClient.Verify(client => client.AddCustomLogJsonAsync("SecurityScanReport", It.IsAny<string>(), It.IsAny<string>()),
                Times.AtLeastOnce());
        }

        [Fact]
        public async Task RunShouldGetAllAzDoProjects()
        {
            Fixture fixture = new Fixture();

            var logAnalyticsClient = new Mock<ILogAnalyticsClient>();
            var vstsClient = new Mock<IVstsRestClient>(MockBehavior.Strict);

            vstsClient.Setup(client => client.Get(It.IsAny<IVstsRestRequest<Response.Multiple<Response.Project>>>()))
                .Returns(fixture.Create<Response.Multiple<Response.Project>>());

            vstsClient.Setup(client => client.Get(It.IsAny<IVstsRestRequest<Response.ApplicationGroups>>()))
                .Returns(fixture.Create<Response.ApplicationGroups>());

            await SecurityScanFunction.Run(new TimerInfo(null, null, false), logAnalyticsClient.Object, vstsClient.Object, new Mock<ILogger>().Object);

            vstsClient.Verify(client => client.Get(It.IsAny<IVstsRestRequest<Response.Multiple<Response.Project>>>()),
                Times.AtLeastOnce());
        }

       

    }
}