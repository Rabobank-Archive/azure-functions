using System;
using Functions.Orchestrators;
using Moq;
using Response = SecurePipelineScan.VstsService.Response;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Functions.Tests.Helpers;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

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
            var orchestrationClientMock = new Mock<IDurableOrchestrationContext>();
            orchestrationClientMock.Setup(
                x => x.GetInput<List<Response.Project>>()).Returns(projects);

            //Act
            var fun = new ProjectScanSupervisor();
            await fun.RunAsync(orchestrationClientMock.Object);

            //Assert
            orchestrationClientMock.Verify(
                x => x.CallSubOrchestratorAsync<object>(nameof(ProjectScanOrchestrator), It.IsAny<string>(), It.IsAny<object>()),
                Times.Exactly(count));
        }
    }
}
