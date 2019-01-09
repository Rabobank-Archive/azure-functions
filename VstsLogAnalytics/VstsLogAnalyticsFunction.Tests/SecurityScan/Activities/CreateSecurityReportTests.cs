using System;
using System.Threading.Tasks;
using AutoFixture;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Moq;
using Rules.Reports;
using SecurePipelineScan.Rules;
using SecurePipelineScan.VstsService.Response;
using VstsLogAnalytics.Client;
using VstsLogAnalyticsFunction.SecurityScan.Activites;
using Xunit;

namespace VstsLogAnalyticsFunction.Tests.SecurityScan.Activities
{
    public class CreateSecurityReportTest
    {
        [Fact]
        public async Task RunShouldCallSecurityReportScanExecute()
        {
            //Arrange
            var fixture = new Fixture();
            
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
            await CreateSecurityReport.Run(
                durableActivityContextBaseMock.Object, 
                durableOrchestrationClient.Object,
                logAnalyticsClient.Object, 
                scan.Object,
                iLoggerMock.Object);

            //Assert
            scan
                .Verify(x => x.Execute(It.IsAny<string>(), It.IsAny<DateTime>()) ,Times.AtLeastOnce());
            logAnalyticsClient
                .Verify(x => x.AddCustomLogJsonAsync("SecurityScanReport", It.IsAny<SecurityReport>(), It.IsAny<string>()));
        }

        [Fact]
        public async Task RunWithNoProjectFoundFromContextShouldThrowException()
        {
            //Arrange
            var scan = new Mock<IProjectScan<SecurityReport>>(MockBehavior.Strict);
            var logAnalyticsClientMock = new Mock<ILogAnalyticsClient>();
            var durableActivityContextBaseMock = new Mock<DurableActivityContextBase>();
            var durableOrchestrationClient = new Mock<DurableOrchestrationClientBase>();
            var iLoggerMock = new Mock<ILogger>();

            //Act
            var ex = await Assert.ThrowsAsync<Exception>(async () => await CreateSecurityReport.Run(
                durableActivityContextBaseMock.Object, 
                durableOrchestrationClient.Object,
                logAnalyticsClientMock.Object,
                scan.Object,
                iLoggerMock.Object));
            
            //Assert
            Assert.Equal("No Project found in parameter DurableActivityContextBase", ex.Message);
        }

        [Fact]

        public async Task RunWithNullLogAnalyticsClientShouldThrowException()
        {
            //Arrange
            var scan = new Mock<IProjectScan<SecurityReport>>(MockBehavior.Strict);
            var durableActivityContextBaseMock = new Mock<DurableActivityContextBase>();
            var durableOrchestrationClient = new Mock<DurableOrchestrationClientBase>();
            var iLoggerMock = new Mock<ILogger>();

            //Act
            var ex = await Assert.ThrowsAsync<ArgumentNullException>(async () => await CreateSecurityReport.Run(
                    durableActivityContextBaseMock.Object,
                    durableOrchestrationClient.Object,
                    null,
                    scan.Object,
                    iLoggerMock.Object));            
            //Assert
            Assert.Equal("Value cannot be null.\nParameter name: logAnalyticsClient", ex.Message);
        }
        
        [Fact]
        public async Task RunWithNullIVstsRestClientShouldThrowException()
        {
            //Arrange
            var logAnalyticsClientMock = new Mock<ILogAnalyticsClient>();
            var durableActivityContextBaseMock = new Mock<DurableActivityContextBase>();
            var durableOrchestrationClient = new Mock<DurableOrchestrationClientBase>();
            var iLoggerMock = new Mock<ILogger>();

            //Act
            var ex = await Assert.ThrowsAsync<ArgumentNullException>(async () => await CreateSecurityReport.Run(
                    durableActivityContextBaseMock.Object, 
                    durableOrchestrationClient.Object,
                    logAnalyticsClientMock.Object,
                    null,
                    iLoggerMock.Object));            
            //Assert
            Assert.Equal("Value cannot be null.\nParameter name: scan", ex.Message);
        }
        
        [Fact]
        public async Task RunWithNullDurableActivityContextShouldThrowException()
        {
            //Arrange
            var scan = new Mock<IProjectScan<SecurityReport>>(MockBehavior.Strict);
            var durableOrchestrationClient = new Mock<DurableOrchestrationClientBase>();
            var logAnalyticsClientMock = new Mock<ILogAnalyticsClient>();
            var iLoggerMock = new Mock<ILogger>();

            //Act
            var ex = await Assert.ThrowsAsync<ArgumentNullException>(async () => await CreateSecurityReport.Run(
                    null, 
                    durableOrchestrationClient.Object,
                    logAnalyticsClientMock.Object,
                    scan.Object,
                    iLoggerMock.Object));            
            //Assert
            Assert.Equal("Value cannot be null.\nParameter name: context", ex.Message);
        }
    }
}