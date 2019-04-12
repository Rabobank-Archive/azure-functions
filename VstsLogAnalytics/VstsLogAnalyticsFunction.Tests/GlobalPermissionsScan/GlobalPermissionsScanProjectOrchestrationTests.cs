using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Moq;
using SecurePipelineScan.VstsService.Response;
using VstsLogAnalyticsFunction.GlobalPermissionsScan;
using Xunit;

namespace VstsLogAnalyticsFunction.Tests.GlobalPermissionsScan
{
    public class GlobalPermissionsScanProjectOrchestrationTests
    {
      
        [Theory]
        [InlineData(2)]
        [InlineData(9)]
        [InlineData(10)]
        [InlineData(11)]
        
        public async System.Threading.Tasks.Task RunWithHasTwoProjectsShouldCallActivityAsyncForEachProject(int numberOfProjects)
        {
            //Arrange
            var durableOrchestrationContextMock = new Mock<DurableOrchestrationContextBase>();
            durableOrchestrationContextMock.Setup(context => context.GetInput<List<Project>>()).Returns(ProjectsTestHelper.CreateMultipleProjectsResponse(numberOfProjects).ToList());

            //Act

            var fun = new GlobalPermissionsScanProjectOrchestration();
            await fun.Run(durableOrchestrationContextMock.Object, new Mock<ILogger>().Object);
            
            //Assert
            durableOrchestrationContextMock.Verify(x => x.CallActivityAsync(nameof(GlobalPermissionsScanProjectActivity), It.IsAny<Project>()),
                Times.Exactly(numberOfProjects));
        }
      
        
        
    }
}