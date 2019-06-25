using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using Functions.Activities;
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
            var fixture = new Fixture();
            var durableOrchestrationContextMock = new Mock<DurableOrchestrationContextBase>();
            
            durableOrchestrationContextMock.Setup(context => context.GetInput<Project>()).Returns(new Project());
            durableOrchestrationContextMock.Setup(context => context.CallActivityAsync<GlobalPermissionsExtensionData>
                (nameof(GlobalPermissionsScanProjectActivity), It.IsAny<Project>())).ReturnsAsync(fixture.Create<GlobalPermissionsExtensionData>());

            //Act
            var fun = new GlobalPermissionsScanProjectOrchestration();
            await fun.Run(durableOrchestrationContextMock.Object);
            
            //Assert
            durableOrchestrationContextMock.Verify(x => x.CallActivityAsync<GlobalPermissionsExtensionData>(nameof(GlobalPermissionsScanProjectActivity), It.IsAny<Project>()),
                Times.Once);
            durableOrchestrationContextMock.Verify(x => x.CallActivityAsync(nameof(ExtensionDataUploadActivity), It.IsAny<ExtensionDataUploadActivityRequest>()),
                Times.Once);
            durableOrchestrationContextMock.Verify(x => x.CallActivityAsync(nameof(LogAnalyticsUploadActivity), It.IsAny<LogAnalyticsUploadActivityRequest>()),
                Times.Once);
        }
    }
}