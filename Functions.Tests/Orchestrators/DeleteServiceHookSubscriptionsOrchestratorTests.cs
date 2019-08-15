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

namespace Functions.Tests.Orchestrators
{
    public class DeleteServiceHookSubscriptionsOrchestratorTests
    {
        private readonly Fixture _fixture;

        public DeleteServiceHookSubscriptionsOrchestratorTests()
        {
            _fixture = new Fixture();
        }
        
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        public async Task RunShouldCallActivityOnceForEveryHook(int count)
        {
            //Arrange       
            var hooks = _fixture.CreateMany<Response.Hook>(count).ToList();
            var orchestrationClientMock = new Mock<DurableOrchestrationContextBase>();
            orchestrationClientMock.Setup(
                x => x.GetInput<List<Response.Hook>>()).Returns(hooks);

            //Act
            var fun = new DeleteServiceHooksSubscriptionsOrchestrator();
            await fun.Run(orchestrationClientMock.Object);

            //Assert
            orchestrationClientMock.Verify(
                x => x.CallActivityAsync(nameof(DeleteServiceHookSubscriptionActivity), It.IsAny<Response.Hook>()),
                Times.Exactly(count));
        }
    }
}
