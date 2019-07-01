using System.Threading.Tasks;
using AutoFixture;
using Functions.Orchestrators;
using Microsoft.Azure.WebJobs;
using Moq;
using Response = SecurePipelineScan.VstsService.Response;
using Xunit;

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
                .Setup(x => x.CallSubOrchestratorAsync(nameof(GlobalPermissionsOrchestration), It.IsAny<Response.Project>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            starter
                .Setup(x => x.CallSubOrchestratorAsync(nameof(RepositoriesOrchestration), It.IsAny<Response.Project>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            starter
                .Setup(x => x.CallSubOrchestratorAsync(nameof(BuildPipelinesOrchestration), It.IsAny<Response.Project>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            starter
                .Setup(x => x.CallSubOrchestratorAsync(nameof(ReleasePipelinesOrchestration), It.IsAny<Response.Project>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            //Act
            var fun = new ProjectScanOrchestration();
            await fun.Run(starter.Object);
            
            //Assert           
            mocks.VerifyAll();
        }
    }
}