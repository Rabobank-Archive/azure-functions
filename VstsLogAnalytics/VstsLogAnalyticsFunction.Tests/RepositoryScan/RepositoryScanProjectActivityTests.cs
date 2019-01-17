using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Moq;
using Rules.Reports;
using SecurePipelineScan.Rules;
using SecurePipelineScan.VstsService.Response;
using VstsLogAnalytics.Client;
using VstsLogAnalyticsFunction.RepositoryScan;
using VstsLogAnalyticsFunction.SecurityScan.Activites;
using Xunit;

namespace VstsLogAnalyticsFunction.Tests.RepositoryScan
{
    public class RepositoryScanProjectActivityTests
    {
        [Fact]
        public async Task GivenMultipleReposAllReposShouldBeSentToLogAnalytics()
        {
            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());

            //Arrange
            var contextMock = new Mock<DurableActivityContextBase>();
            contextMock
                       .Setup(x => x.GetInput<Project>())
                       .Returns(fixture.Create<Project>);

            var scan = new Mock<IProjectScan<IEnumerable<RepositoryReport>>>(MockBehavior.Strict);
            scan
                .Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns(fixture.CreateMany<RepositoryReport>());

            var logAnalyticsClientMock = new Mock<ILogAnalyticsClient>();
            var durableOrchestrationClient = new Mock<DurableOrchestrationClientBase>();
            var iLoggerMock = new Mock<ILogger>();

            //Act
            await  RepositoryScanProjectActivity.Run(
                contextMock.Object,
                logAnalyticsClientMock.Object,
                scan.Object,
                iLoggerMock.Object);

            //Assert
            logAnalyticsClientMock.Verify(x => 
              x.AddCustomLogJsonAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>()), Times.AtLeastOnce());
        }

        [Fact]
        public async Task RunWithNoLogAnalyticsClientShouldThrowArgumentNullException()
        {
            //Arrange
            var scan = new Mock<IProjectScan<IEnumerable<RepositoryReport>>>(MockBehavior.Strict);
            var durableActivityContextBaseMock = new Mock<DurableActivityContextBase>();
            var durableOrchestrationClient = new Mock<DurableOrchestrationClientBase>();
            var iLoggerMock = new Mock<ILogger>();

            //Act + assert
            var ex = await Assert.ThrowsAsync<ArgumentNullException>(async () => await RepositoryScanProjectActivity.Run(
                durableActivityContextBaseMock.Object,
                null,
                scan.Object,
                iLoggerMock.Object));
        }

        [Fact]
        public async Task RunWithNoScannerShouldThrowArgumentNullException()
        {
            //Arrange
            var logAnalyticsClientMock = new Mock<ILogAnalyticsClient>();
            var durableActivityContextBaseMock = new Mock<DurableActivityContextBase>();
            var durableOrchestrationClient = new Mock<DurableOrchestrationClientBase>();
            var iLoggerMock = new Mock<ILogger>();

            //Act + assert
            var ex = await Assert.ThrowsAsync<ArgumentNullException>(async () => await RepositoryScanProjectActivity.Run(
                durableActivityContextBaseMock.Object,
                logAnalyticsClientMock.Object,
                null,
                iLoggerMock.Object));
        }
    }
       
}