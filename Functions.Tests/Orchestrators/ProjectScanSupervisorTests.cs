using Functions.Orchestrators;
using Microsoft.Azure.WebJobs;
using Moq;
using Response = SecurePipelineScan.VstsService.Response;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

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
            await fun.Run(orchestrationClientMock.Object);

            //Assert
            orchestrationClientMock.Verify(
                x => x.CallSubOrchestratorAsync(nameof(ProjectScanOrchestration), It.IsAny<object>()),
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
            await fun.Run(orchestrationClientMock.Object);

            //Assert
            orchestrationClientMock.Verify(
                x => x.SetCustomStatus(It.Is<SupervisorOrchestrationStatus>(y => y.TotalProjectCount == count)));
        }
    }
}
