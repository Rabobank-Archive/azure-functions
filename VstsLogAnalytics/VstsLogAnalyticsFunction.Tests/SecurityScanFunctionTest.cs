using System;
using System.Collections;
using System.Collections.Generic;
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
        public void productionOwnerResponseShouldBeSentToLogAnalytics()
        {
            Fixture fixture = new Fixture();

            var logAnalyticsClient = new Mock<ILogAnalyticsClient>();
            var vstsClient = new Mock<IVstsRestClient>(MockBehavior.Strict);

            vstsClient.Setup(client => client.Execute(It.IsAny<IVstsRestRequest<Response.Multiple<SecurePipelineScan.VstsService.Response.Project>>>()))
                .Returns(new RestResponse<Response.Multiple<SecurePipelineScan.VstsService.Response.Project>>
                    {Data = fixture.Create<Response.Multiple<SecurePipelineScan.VstsService.Response.Project>>()});

            vstsClient.Setup(client => client.Execute(It.IsAny<IVstsRestRequest<Response.ApplicationGroups>>())).Returns(
                new RestResponse<Response.ApplicationGroups>
                    {Data = fixture.Create<Response.ApplicationGroups>()});

            SecurityScanFunction.Run(new TimerInfo(null, null, false), logAnalyticsClient.Object, vstsClient.Object, new Mock<ILogger>().Object);

            logAnalyticsClient.Verify(client => client.AddCustomLogJsonAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce());
        }

        [Fact]
        public void logNameInAddCustomLogJsonAsyncShouldBeSecurityScanReport()
        {
            Fixture fixture = new Fixture();

            var logAnalyticsClient = new Mock<ILogAnalyticsClient>();
            var vstsClient = new Mock<IVstsRestClient>(MockBehavior.Strict);

            vstsClient.Setup(client => client.Execute(It.IsAny<IVstsRestRequest<Response.Multiple<SecurePipelineScan.VstsService.Response.Project>>>()))
                .Returns(new RestResponse<Response.Multiple<SecurePipelineScan.VstsService.Response.Project>>
                    {Data = fixture.Create<Response.Multiple<SecurePipelineScan.VstsService.Response.Project>>()});

            vstsClient.Setup(client => client.Execute(It.IsAny<IVstsRestRequest<Response.ApplicationGroups>>()))
                .Returns(new RestResponse<Response.ApplicationGroups> {Data = fixture.Create<Response.ApplicationGroups>()});

            SecurityScanFunction.Run(new TimerInfo(null, null, false), logAnalyticsClient.Object, vstsClient.Object, new Mock<ILogger>().Object);

            logAnalyticsClient.Verify(client => client.AddCustomLogJsonAsync("SecurityScanReport", It.IsAny<string>(), It.IsAny<string>()),
                Times.AtLeastOnce());
        }

        [Fact]
        public void runShouldGetAllAzDoProjects()
        {
            Fixture fixture = new Fixture();

            var logAnalyticsClient = new Mock<ILogAnalyticsClient>();
            var vstsClient = new Mock<IVstsRestClient>(MockBehavior.Strict);

            vstsClient.Setup(client => client.Execute(It.IsAny<IVstsRestRequest<Response.Multiple<Response.Project>>>()))
                .Returns(new RestResponse<Response.Multiple<Response.Project>> {Data = fixture.Create<Response.Multiple<Response.Project>>()});

            SecurityScanFunction.Run(new TimerInfo(null, null, false), logAnalyticsClient.Object, vstsClient.Object, new Mock<ILogger>().Object);

            vstsClient.Verify(client => client.Execute(It.IsAny<IVstsRestRequest<Response.Multiple<Response.Project>>>()),
                Times.AtLeastOnce());
        }

       

    }
}