using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.Azure.WebJobs;
using Moq;
using SecurePipelineScan.Rules;
using SecurePipelineScan.Rules.Reports;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
using System;
using VstsLogAnalytics.Client;
using Xunit;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace VstsLogAnalyticsFunction.Tests.SecurityScan.Activities
{
    public class SecurityScanProjectActivityTest
    {
        [Fact]
        public async System.Threading.Tasks.Task RunShouldCallSecurityReportScanExecute()
        {
            //Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());

            IVstsRestClient client = fixture.Create<IVstsRestClient>();

            var logAnalyticsClient = new Mock<ILogAnalyticsClient>();
            var iLoggerMock = new Mock<ILogger>();
            var scan = new Mock<IProjectScan<SecurityReport>>();
            scan
                .Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns(fixture.Create<SecurityReport>());

            var durableActivityContextBaseMock = new Mock<DurableActivityContextBase>();
            durableActivityContextBaseMock
                .Setup(x => x.GetInput<Project>())
                .Returns(fixture.Create<Project>());

            var durableOrchestrationClient = new Mock<DurableOrchestrationClientBase>();
            durableOrchestrationClient
                .Setup(x => x.GetStatusAsync(It.IsAny<string>()))
                .ReturnsAsync(fixture.Build<DurableOrchestrationStatus>()
                    .Without(x => x.Input)
                    .Without(x => x.Output)
                    .Without(x => x.History)
                    .Without(x => x.CustomStatus)
                    .Create());

            //Act
            SecurityScanProjectActivity fun = new SecurityScanProjectActivity(logAnalyticsClient.Object, scan.Object, client, fixture.Create<IAzureDevOpsConfig>());
            await fun.Run(
                durableActivityContextBaseMock.Object,
                iLoggerMock.Object);

            //Assert
            scan
                .Verify(x => x.Execute(It.IsAny<string>(), It.IsAny<DateTime>()), Times.AtLeastOnce());
            logAnalyticsClient
                .Verify(x => x.AddCustomLogJsonAsync("SecurityScanReport", It.IsAny<SecurityReport>(), It.IsAny<string>()));
        }

        [Fact]
        public async System.Threading.Tasks.Task RunWithNoProjectFoundFromContextShouldThrowException()
        {
            //Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var scan = new Mock<IProjectScan<SecurityReport>>(MockBehavior.Strict);
            var logAnalyticsClient = new Mock<ILogAnalyticsClient>();
            var durableActivityContextBaseMock = new Mock<DurableActivityContextBase>();
            var iLoggerMock = new Mock<ILogger>();
            var client = new Mock<IVstsRestClient>().Object;

            //Act
            SecurityScanProjectActivity fun = new SecurityScanProjectActivity(logAnalyticsClient.Object, scan.Object, client, fixture.Create<IAzureDevOpsConfig>());

            var ex = await Assert.ThrowsAsync<Exception>(async () => await fun.Run(
                durableActivityContextBaseMock.Object,
                iLoggerMock.Object));

            //Assert
            Assert.Equal("No Project found in parameter DurableActivityContextBase", ex.Message);
        }
    }
}