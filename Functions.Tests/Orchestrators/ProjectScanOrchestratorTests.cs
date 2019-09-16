using AutoFixture;
using Functions.Activities;
using Functions.Model;
using Functions.Orchestrators;
using Microsoft.Azure.WebJobs;
using Moq;
using System.Threading.Tasks;
using Xunit;
using Response = SecurePipelineScan.VstsService.Response;

namespace Functions.Tests.Orchestrators
{
    public class ProjectScanOrchestratorTests
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
                .Setup(x => x.InstanceId)
                .Returns(fixture.Create<string>());

            starter
                .Setup(x => x.CallActivityWithRetryAsync<ItemOrchestratorRequest>(
                    nameof(GetDataFromTableStorageActivity), It.IsAny<RetryOptions>(), It.IsAny<Response.Project>()))
                .ReturnsAsync(fixture.Create<ItemOrchestratorRequest>())
                .Verifiable();

            starter
                .Setup(x => x.CallSubOrchestratorAsync(
                    nameof(GlobalPermissionsOrchestration), It.IsAny<string>(), It.IsAny<ItemOrchestratorRequest>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            starter
                .Setup(x => x.CallSubOrchestratorAsync<(ItemOrchestratorRequest, ItemOrchestratorRequest)>(
                    nameof(ReleasePipelinesOrchestration), It.IsAny<string>(), It.IsAny<ItemOrchestratorRequest>()))
                .ReturnsAsync((fixture.Create<ItemOrchestratorRequest>(), fixture.Create<ItemOrchestratorRequest>()))
                .Verifiable();

            starter
                .Setup(x => x.CallSubOrchestratorAsync(
                    nameof(RepositoriesOrchestration), It.IsAny<string>(), It.IsAny<ItemOrchestratorRequest>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            starter
                .Setup(x => x.CallSubOrchestratorAsync(
                    nameof(BuildPipelinesOrchestration), It.IsAny<string>(), It.IsAny<ItemOrchestratorRequest>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            //Act
            var fun = new ProjectScanOrchestration();
            await fun.RunAsync(starter.Object);

            //Assert           
            mocks.VerifyAll();
        }
    }
}