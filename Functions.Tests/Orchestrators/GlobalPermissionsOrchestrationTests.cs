using System.Threading.Tasks;
using AutoFixture;
using Functions.Activities;
using Functions.Model;
using Functions.Orchestrators;
using Microsoft.Azure.WebJobs;
using Moq;
using Response = SecurePipelineScan.VstsService.Response;
using Xunit;

namespace Functions.Tests.Orchestrators
{
    public class GlobalPermissionsOrchestrationTests
    {
      
        [Fact]
        public async Task ShouldCallActivityAsyncForProject()
        {
            //Arrange
            var fixture = new Fixture();
            var mocks = new MockRepository(MockBehavior.Strict);
            
            var starter = mocks.Create<DurableOrchestrationContextBase>();
            starter
                .Setup(x => x.GetInput<Response.Project>())
                .Returns(fixture.Create<Response.Project>());
            
            starter
                .Setup(x => x.SetCustomStatus(It.IsAny<object>()));
            
            starter
                .Setup(x => x.CallActivityAsync<GlobalPermissionsExtensionData>(nameof(GlobalPermissionsScanProjectActivity), It.IsAny<Response.Project>()))
                .ReturnsAsync(fixture.Create<GlobalPermissionsExtensionData>())
                .Verifiable();
            
            starter
                .Setup(x => x.CallActivityAsync(nameof(ExtensionDataGlobalPermissionsUploadActivity), It.IsAny<object>()))
                .Returns(Task.CompletedTask);

            starter
                .Setup(x => x.CallActivityAsync(nameof(LogAnalyticsUploadActivity), It.IsAny<LogAnalyticsUploadActivityRequest>()))
                .Returns(Task.CompletedTask);

            //Act
            await GlobalPermissionsOrchestration.Run(starter.Object);
            
            //Assert           
            mocks.VerifyAll();
        }
    }
}