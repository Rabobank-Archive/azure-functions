using System;
using Functions.Orchestrators;
using Microsoft.Azure.WebJobs;
using Moq;
using Response = SecurePipelineScan.VstsService.Response;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using AzDoCompliancy.CustomStatus;

namespace Functions.Tests.Orchestrators
{
    public class ProjectScanSupervisorTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        public async Task RunShouldCallOrchestratorFunctionOnceForEveryProject(int count)
        {
            //Arrange       
            var projects = ProjectsTestHelper.CreateMultipleProjectsResponse(count).ToList();
            var orchestrationClientMock = new Mock<DurableOrchestrationContextBase>();
            orchestrationClientMock.Setup(
                x => x.GetInput<List<Response.Project>>()).Returns(projects);

            //Act
            var fun = new ProjectScanSupervisor();
            await fun.RunAsync(orchestrationClientMock.Object);

            //Assert
            orchestrationClientMock.Verify(
                x => x.CallSubOrchestratorAsync(nameof(ProjectScanOrchestrator), It.IsAny<string>(), It.IsAny<object>()),
                Times.Exactly(count));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        public async Task RunShouldSetCustomStateWithTotalProjectCount(int count)
        {
            //Arrange       
            var projects = ProjectsTestHelper.CreateMultipleProjectsResponse(count).ToList();
            var orchestrationClientMock = new Mock<DurableOrchestrationContextBase>();
            orchestrationClientMock.Setup(
                x => x.GetInput<List<Response.Project>>()).Returns(projects);

            //Act
            var fun = new ProjectScanSupervisor();
            await fun.RunAsync(orchestrationClientMock.Object);

            //Assert
            orchestrationClientMock.Verify(
                x => x.SetCustomStatus(It.Is<SupervisorOrchestrationStatus>(y => y.TotalProjectCount == count)));
        }

        [Fact]
        public async Task SubOrchestrationIdShouldIncludeProjectId()
        {
            //Arrange       
            var projects = ProjectsTestHelper.CreateMultipleProjectsResponse(1).ToList();
            var orchestrationClientMock = new Mock<DurableOrchestrationContextBase>();
            orchestrationClientMock.Setup(
                x => x.GetInput<List<Response.Project>>()).Returns(projects);

            //Act
            var fun = new ProjectScanSupervisor();
            await fun.RunAsync(orchestrationClientMock.Object);

            //Assert
            orchestrationClientMock.Verify(
                x => x.CallSubOrchestratorAsync(nameof(ProjectScanOrchestrator),
                    It.Is<string>(i => i.Contains(projects.First().Id)), It.IsAny<object>()));
        }
        
        [Fact]
        public async Task SubOrchestrationIdShouldIncludeParentOrchestrationId()
        {
            //Arrange       
            var projects = ProjectsTestHelper.CreateMultipleProjectsResponse(1).ToList();
            var orchestrationClientMock = new Mock<DurableOrchestrationContextBase>();
            orchestrationClientMock.Setup(
                x => x.GetInput<List<Response.Project>>()).Returns(projects);
            orchestrationClientMock.SetupGet(x => x.InstanceId).Returns(Guid.NewGuid().ToString());

            //Act
            var fun = new ProjectScanSupervisor();
            await fun.RunAsync(orchestrationClientMock.Object);

            //Assert
            orchestrationClientMock.Verify(
                x => x.CallSubOrchestratorAsync(nameof(ProjectScanOrchestrator),
                    It.Is<string>(i => i.Contains(orchestrationClientMock.Object.InstanceId)), It.IsAny<object>()));
        }
    }
}
