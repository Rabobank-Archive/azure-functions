using AutoFixture;
using Functions.Activities;
using Functions.Model;
using Functions.Orchestrators;
using Microsoft.Azure.WebJobs;
using Moq;
using System.Collections.Generic;
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
                .Setup(x => x.CallActivityWithRetryAsync<IList<ProductionItem>>(
                    nameof(GetDeploymentMethodsActivity), It.IsAny<RetryOptions>(), 
                    It.IsAny<Response.Project>()))
                .ReturnsAsync(fixture.Create<IList<ProductionItem>>())
                .Verifiable();

            starter
                .Setup(x => x.CallSubOrchestratorAsync(
                    nameof(GlobalPermissionsOrchestrator), It.IsAny<string>(), 
                    It.IsAny<(Response.Project, IList<ProductionItem>)>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            starter
                .Setup(x => x.CallSubOrchestratorAsync<IList<ProductionItem>>(
                    nameof(ReleasePipelinesOrchestrator), It.IsAny<string>(), 
                    It.IsAny<(Response.Project, IList<ProductionItem>)>()))
                .ReturnsAsync(fixture.Create<IList<ProductionItem>>())
                .Verifiable();

            starter
                .Setup(x => x.CallSubOrchestratorAsync<IList<ProductionItem>>(
                    nameof(BuildPipelinesOrchestrator), It.IsAny<string>(), 
                    It.IsAny<(Response.Project, IList<ProductionItem>)>()))
                .ReturnsAsync(fixture.Create<IList<ProductionItem>>())
                .Verifiable();

            starter
                .Setup(x => x.CallSubOrchestratorAsync(
                    nameof(RepositoriesOrchestrator), It.IsAny<string>(), 
                    It.IsAny<(Response.Project, IList<ProductionItem>)>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            //Act
            var fun = new ProjectScanOrchestrator();
            await fun.RunAsync(starter.Object);

            //Assert           
            mocks.VerifyAll();
        }
    }
}