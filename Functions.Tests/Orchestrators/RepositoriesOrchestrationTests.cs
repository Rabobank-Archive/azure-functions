using System.Threading.Tasks;
using AutoFixture;
using Functions.Activities;
using Functions.Model;
using Functions.Orchestrators;
using Microsoft.Azure.WebJobs;
using Moq;
using Xunit;

namespace Functions.Tests.Orchestrators
{
    public class RepositoriesOrchestrationTests
    {
      
        [Fact]
        public async Task ShouldCallActivityAsyncForProject()
        {
            //Arrange
            var fixture = new Fixture();
            var mocks = new MockRepository(MockBehavior.Strict);
            
            var starter = mocks.Create<DurableOrchestrationContextBase>();
            starter
                .Setup(x => x.GetInput<string>())
                .Returns(fixture.Create<string>());
            
            starter
                .Setup(x => x.SetCustomStatus(It.IsAny<object>()));
            
            starter
                .Setup(x => x.CallActivityAsync<ItemsExtensionData>(nameof(RepositoriesScanActivity), It.IsAny<string>()))
                .ReturnsAsync(fixture.Create<ItemsExtensionData>())
                .Verifiable();

            starter
                .Setup(x => x.CallActivityAsync(nameof(ExtensionDataUploadActivity), It.IsAny<object>()))
                .Returns(Task.CompletedTask);

            starter
                .Setup(x => x.CallActivityAsync(nameof(LogAnalyticsUploadActivity), It.IsAny<LogAnalyticsUploadActivityRequest>()))
                .Returns(Task.CompletedTask);

            //Act
            await RepositoriesOrchestration.Run(starter.Object);
            
            //Assert           
            mocks.VerifyAll();
        }
    }
}