using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Build.Framework;
using Moq;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
using VstsLogAnalytics.Client;
using VstsLogAnalyticsFunction.SecurityScan.Activites;
using ApplicationGroup = SecurePipelineScan.VstsService.Requests;
using Xunit;
using Xunit.Abstractions;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using Project = SecurePipelineScan.VstsService.Response.Project;
using Response = SecurePipelineScan.VstsService.Response;


namespace VstsLogAnalyticsFunction.Tests.SecurityScan.Activities
{
    public class CreateSecurityReportTest
    {
        [Fact]
        public void RunShouldCallAddCustomLogJsonAsync()
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
            CreateSecurityReport.Run(durableActivityContextBaseMock.Object, logAnalyticsClient.Object, client.Object, iLoggerMock.Object);

            //Assert
            logAnalyticsClient.Verify(x => x.AddCustomLogJsonAsync("SecurityScanReport", It.IsAny<object>(), It.IsAny<string>()),
                Times.AtLeastOnce());
        }

        [Fact]
        public void RunShouldCallGetApplicationGroups()
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
            CreateSecurityReport.Run(durableActivityContextBaseMock.Object, logAnalyticsClientMock.Object, clientMock.Object, iLoggerMock.Object);

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
            var applicationGroup1 = new Response.ApplicationGroup {DisplayName = "[dummy]\\Project Administrators", TeamFoundationId = "1234",};
            var applicationGroup2 = new Response.ApplicationGroup {DisplayName = "[TAS]\\Rabobank Project Administrators"};
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

        private static Multiple<Project> CreateProjectsResponse()
        {
            var project1 = new Project
            {
                Id = "1",
                Name = "TAS"
            };

            var project2 = new Project
            {
                Id = "2",
                Name = "TASSIE"
            };
            var allProjects = new Multiple<Project>(project1, project2);
            return allProjects;
        }
    }
}