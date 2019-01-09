using System;
using System.Threading.Tasks;
using AutoFixture;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Moq;
using SecurePipelineScan.Rules;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
using VstsLogAnalytics.Client;
using VstsLogAnalyticsFunction.SecurityScan.Activites;
using Xunit;
using ApplicationGroup = SecurePipelineScan.VstsService.Requests;
using Response = SecurePipelineScan.VstsService.Response;
using Requests = SecurePipelineScan.VstsService.Requests;


namespace VstsLogAnalyticsFunction.Tests.SecurityScan.Activities
{
    public class CreateSecurityReportTest
    {
//        [Fact]
//        public async Task RunShouldCallSecurityReportScanExecute()
//        {
//            DISABLED, CAN NOT MOCK SECURITYREPORTSCAN (MUST CREATE INTERFACE FIRST)
//            //Arrange
//            var fixture = new Fixture();
//            
//            var client = new Mock<IVstsRestClient>(MockBehavior.Strict);
//            var logAnalyticsClient = new Mock<ILogAnalyticsClient>();
//            var durableActivityContextBaseMock = new Mock<DurableActivityContextBase>();
//            var iLoggerMock = new Mock<ILogger>();
//            var securityCanMock = new Mock<SecurityReportScan>(client);
//
//            var testProject = CreateMockProject();
//            var applicationGroups = CreateApplicationGroupsMock();
//            
//            var names = new Multiple<SecurityNamespace>(new SecurityNamespace
//            {
//                DisplayName = "Git Repositories",
//                NamespaceId = "123456"
//            });
//
//            client.Setup(x => x.Get(It.IsAny<IVstsRestRequest<ApplicationGroups>>())).Returns(applicationGroups);
//            durableActivityContextBaseMock.Setup(x => x.GetInput<Project>()).Returns(testProject);
//            
//            client.Setup(x => x.Get(It.IsAny<IVstsRestRequest<Multiple<SecurityNamespace>>>())).Returns(names);
//            client.Setup(x => x.Get(It.IsAny<IVstsRestRequest<ProjectProperties>>())).Returns(fixture.Create<ProjectProperties>());
//            client.Setup(x => x.Get(It.IsAny<IVstsRestRequest<PermissionsSetId>>())).Returns(fixture.Create<PermissionsSetId>());
//            client.Setup(x => x.Get(It.IsAny<IVstsRestRequest<Multiple<Repository>>>())).Returns(fixture.Create<Multiple<Repository>>());
//
//            //Act
//            await CreateSecurityReport.Run(durableActivityContextBaseMock.Object, logAnalyticsClient.Object, client.Object, iLoggerMock.Object);
//
//            //Assert
//            securityCanMock.Verify(x => x.Execute(It.IsAny<Project>().Name) ,Times.AtLeastOnce());
//        }

        [Fact]
        public async Task RunWithNoProjectFoundFromContextShouldThrowException()
        
        {
            //Arrange
            var clientMock = new Mock<IVstsRestClient>(MockBehavior.Strict);
            var logAnalyticsClientMock = new Mock<ILogAnalyticsClient>();
            var durableActivityContextBaseMock = new Mock<DurableActivityContextBase>();
            var iLoggerMock = new Mock<ILogger>();

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

        [Fact]

        public async Task RunWithNullLogAnalyticsClientShouldThrowException()
        {
            //Arrange
            var clientMock = new Mock<IVstsRestClient>(MockBehavior.Strict);
            var durableActivityContextBaseMock = new Mock<DurableActivityContextBase>();
            var iLoggerMock = new Mock<ILogger>();

            //Act
            try
            {
                await CreateSecurityReport.Run(durableActivityContextBaseMock.Object, null, clientMock.Object, iLoggerMock.Object);
            }
            
            //Assert
            catch(Exception ex)
            {
                Assert.Equal("Value cannot be null.\nParameter name: logAnalyticsClient", ex.Message);
            }
        }
        
        [Fact]
        public async Task RunWithNullIVstsRestClientShouldThrowException()
        {
            //Arrange
            var logAnalyticsClientMock = new Mock<ILogAnalyticsClient>();
            var durableActivityContextBaseMock = new Mock<DurableActivityContextBase>();
            var iLoggerMock = new Mock<ILogger>();

            //Act
            try
            {
                await CreateSecurityReport.Run(durableActivityContextBaseMock.Object, logAnalyticsClientMock.Object, null, iLoggerMock.Object);
            }
            
            //Assert
            catch(Exception ex)
            {
                Assert.Equal("Value cannot be null.\nParameter name: client", ex.Message);
            }
        }
        
        [Fact]
        public async Task RunWithNullDurableActivityContextShouldThrowException()
        {
            //Arrange
            var clientMock = new Mock<IVstsRestClient>(MockBehavior.Strict);
            var logAnalyticsClientMock = new Mock<ILogAnalyticsClient>();
            var iLoggerMock = new Mock<ILogger>();

            //Act
            try
            {
                await CreateSecurityReport.Run(null, logAnalyticsClientMock.Object, clientMock.Object, iLoggerMock.Object);
            }
            
            //Assert
            catch(Exception ex)
            {
                Assert.Equal("Value cannot be null.\nParameter name: context", ex.Message);
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
                Name = "dummy",
                Description = "blabalba",
            };
            return testProject;
        }
    }
}