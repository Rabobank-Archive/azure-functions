using System.Collections.Generic;
using System.Linq;
using Functions.GlobalPermissionsScan;
using Functions.Model;
using Functions.Tests;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Moq;
using SecurePipelineScan.VstsService.Response;
using Xunit;

namespace Functions.Tests.GlobalPermissionsScan
{
    public class GlobalPermissionsScanProjectOrchestrationTests
    {
      
        [Fact]
        public async System.Threading.Tasks.Task ShouldCallActivityAsyncForProject()
        {
            //Arrange
            var durableOrchestrationContextMock = new Mock<DurableOrchestrationContextBase>();
            durableOrchestrationContextMock.Setup(context => context.GetInput<Project>()).Returns(new Project());

            //Act

            var fun = new GlobalPermissionsScanProjectOrchestration();
            await fun.Run(durableOrchestrationContextMock.Object, new Mock<ILogger>().Object);
            
            //Assert
            durableOrchestrationContextMock.Verify(x => x.CallActivityAsync<GlobalPermissionsExtensionData>(nameof(GlobalPermissionsScanProjectActivity), It.IsAny<Project>()),
                Times.Once);
        }
      
        
        
    }
}