using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Moq;
using SecurePipelineScan.VstsService.Response;
using VstsLogAnalyticsFunction.SecurityScan.Activites;
using VstsLogAnalyticsFunction.SecurityScan.Orchestrations;
using Xunit;

namespace VstsLogAnalyticsFunction.Tests.SecurityScan.Orchestrations
{
    public class GetAllProjectTasksTest
    {
        [Fact]
        public async System.Threading.Tasks.Task RunWithHasTwoProjectsShouldCallActivityAsyncForEachProject()
        {
            //Arrange
            var durableOrchestrationContextMock = new Mock<DurableOrchestrationContextBase>();
            durableOrchestrationContextMock.Setup(context => context.GetInput<List<Project>>()).Returns(ProjectsTestHelper.CreateMultipleProjectsResponse().ToList());

            //Act
            await GetAllProjectTasks.Run(durableOrchestrationContextMock.Object, new Mock<ILogger>().Object);
            
            //Assert
            durableOrchestrationContextMock.Verify(x => x.CallActivityAsync<int>(nameof(CreateSecurityReport), It.IsAny<Project>()), Times.Exactly(2));
        }
        
        
        
    }
}