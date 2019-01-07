using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Moq;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
using VstsLogAnalytics.Client;
using VstsLogAnalyticsFunction.SecurityScan.Activites;
using Xunit;
using ApplicationGroup = SecurePipelineScan.VstsService.Requests;


namespace VstsLogAnalyticsFunction.Tests.SecurityScan.Activities
{
    public class CreateSecurityReportTest
    {
        [Fact]
        public async Task RunShouldCallAddCustomLogJsonAsync()
        {
            
            //Arrange
            var client = new Mock<IVstsRestClient>(MockBehavior.Strict);
            var logAnalyticsClient = new Mock<ILogAnalyticsClient>();
            var durableActivityContextBaseMock = new Mock<DurableActivityContextBase>();
            var iLoggerMock = new Mock<ILogger>();

            var testProject = CreateMockProject();
            var applicationGroups = CreateApplicationGroupsMock();

            client.Setup(x => x.Get(It.IsAny<IVstsRestRequest<ApplicationGroups>>())).Returns(applicationGroups);
            durableActivityContextBaseMock.Setup(x => x.GetInput<Project>()).Returns(testProject);

            //Act
            await CreateSecurityReport.Run(durableActivityContextBaseMock.Object, logAnalyticsClient.Object, client.Object, iLoggerMock.Object);

            //Assert
            logAnalyticsClient.Verify(x => x.AddCustomLogJsonAsync("SecurityScanReport", It.IsAny<object>(), It.IsAny<string>()),
                Times.AtLeastOnce());
        }

        [Fact]
        public async Task RunShouldCallGetApplicationGroups()
        {
            //Arrange
            var clientMock = new Mock<IVstsRestClient>(MockBehavior.Strict);
            var logAnalyticsClientMock = new Mock<ILogAnalyticsClient>();
            var durableActivityContextBaseMock = new Mock<DurableActivityContextBase>();
            var iLoggerMock = new Mock<ILogger>();

            var testProject = CreateMockProject();
            var applicationGroups = CreateApplicationGroupsMock();

            clientMock.Setup(x => x.Get(It.IsAny<IVstsRestRequest<ApplicationGroups>>())).Returns(applicationGroups);
            durableActivityContextBaseMock.Setup(x => x.GetInput<Project>()).Returns(testProject);

            //Act
            await CreateSecurityReport.Run(durableActivityContextBaseMock.Object, logAnalyticsClientMock.Object, clientMock.Object, iLoggerMock.Object);

            //Assert
            clientMock.Verify(x => x.Get(It.IsAny<IVstsRestRequest<ApplicationGroups>>()),Times.AtLeastOnce());
        }

        [Fact]
        public async Task RunWithNoProjectFoundFromContextShouldThrowException()
        
        {
            //Arrange
            var clientMock = new Mock<IVstsRestClient>(MockBehavior.Strict);
            var logAnalyticsClientMock = new Mock<ILogAnalyticsClient>();
            var durableActivityContextBaseMock = new Mock<DurableActivityContextBase>();
            var iLoggerMock = new Mock<ILogger>();

            var applicationGroups = CreateApplicationGroupsMock();

            clientMock.Setup(x => x.Get(It.IsAny<IVstsRestRequest<ApplicationGroups>>())).Returns(applicationGroups);
            
            //Act
            try
            {
               await CreateSecurityReport.Run(durableActivityContextBaseMock.Object, logAnalyticsClientMock.Object, clientMock.Object, iLoggerMock.Object);
            }
            
            //Assert
            catch(Exception ex)
            {
                Assert.Equal("No Project found in parameter DurableActivityContextBase", ex.Message);
            }
        }
        
        private static ApplicationGroups CreateApplicationGroupsMock()
        {
            var applicationGroup1 = new SecurePipelineScan.VstsService.Response.ApplicationGroup {DisplayName = "[dummy]\\Project Administrators", TeamFoundationId = "1234",};
            var applicationGroup2 = new SecurePipelineScan.VstsService.Response.ApplicationGroup {DisplayName = "[TAS]\\Rabobank Project Administrators"};
            var applicationGroups = new ApplicationGroups {Identities = new[] {applicationGroup1, applicationGroup2}};
            return applicationGroups;
        }

        private static Project CreateMockProject()
        {
            var testProject = new Project
            {
                Id = "1",
                Name = "Test Project",
                Description = "blabalba",
            };
            return testProject;
        }
    }
}