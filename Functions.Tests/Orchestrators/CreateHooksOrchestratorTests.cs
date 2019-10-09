using Functions.Orchestrators;
using Microsoft.Azure.WebJobs;
using Moq;
using Response = SecurePipelineScan.VstsService.Response;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using Xunit;
using Functions.Activities;
using Functions.Model;

namespace Functions.Tests.Orchestrators
{
    public class CreateHooksOrchestratorTests
    {
        private readonly Fixture _fixture;

        public CreateHooksOrchestratorTests()
        {
            _fixture = new Fixture();
        }
        
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        public async Task RunShouldCallActivitiesOnceForEveryProject(int projectCount)
        {
            //Arrange       
            var projects = _fixture.CreateMany<Response.Project>(projectCount).ToList();

            var orchestrationClientMock = new Mock<DurableOrchestrationContextBase>();
            
            orchestrationClientMock.Setup(
                x => x.CallActivityWithRetryAsync<IList<Response.Project>>(nameof(GetProjectsActivity),
                    It.IsAny<RetryOptions>(), null)).Returns(Task.FromResult((IList<Response.Project>)projects))
                .Verifiable();

            //Act
            var fun = new CreateHooksOrchestrator();
            await fun.RunAsync(orchestrationClientMock.Object);

            //Assert
            orchestrationClientMock.Verify();
            orchestrationClientMock.Verify(x =>
                x.CallActivityWithRetryAsync<IList<Response.Hook>>(nameof(GetHooksActivity), It.IsAny<RetryOptions>(),
                    null));
            orchestrationClientMock.Verify(x => x.CallActivityAsync(nameof(CreateStorageQueuesActivity), null));
            orchestrationClientMock.Verify(
                x => x.CallActivityWithRetryAsync(nameof(CreateHooksActivity),
                    It.IsAny<RetryOptions>(), It.IsAny<(IList<Response.Hook>, Response.Project)>()),
                Times.Exactly(projectCount));
        }
    }
}
