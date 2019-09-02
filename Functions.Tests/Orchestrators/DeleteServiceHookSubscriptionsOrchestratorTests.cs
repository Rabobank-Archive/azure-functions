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
        private const string AccountName = "azdocompliancyqueue";
        private const string AccountKey = "ZHVtbXlrZXk=";

        public DeleteServiceHookSubscriptionsOrchestratorTests()
        {
            _fixture = new Fixture();
        }
        
        [Fact]
        public async Task RunShouldCallGetHooksExactlyOnce()
        {
            //Arrange
            var orchestrationContextMock = new Mock<DurableOrchestrationContextBase>();
            orchestrationContextMock.Setup(x =>
                    x.CallActivityWithRetryAsync<IList<Response.Hook>>(nameof(GetHooksActivity),
                        It.IsAny<RetryOptions>(), null))
                .Returns(Task.FromResult(_fixture.CreateMany<Response.Hook>().ToList() as IList<Response.Hook>));
            var config = new EnvironmentConfig
            {
                EventQueueStorageAccountName = AccountName, EventQueueStorageAccountKey = AccountKey
            };

            //Act
            var fun = new DeleteServiceHookSubscriptionsOrchestrator(config);
            await fun.RunAsync(orchestrationContextMock.Object);

            //Assert
            orchestrationContextMock.Verify(
                x => x.CallActivityWithRetryAsync<IList<Response.Hook>>(nameof(GetHooksActivity),
                    It.IsAny<RetryOptions>(), null), Times.Once);
        }
        
        [Fact]
        public async Task RunShouldCallActivityExactlyOnceForCompliancyHooks()
        {
            //Arrange       
            var orchestrationContextMock = new Mock<DurableOrchestrationContextBase>();
            var config = new EnvironmentConfig
            {
                EventQueueStorageAccountName = AccountName, EventQueueStorageAccountKey = AccountKey
            };

            var hooks = new List<Response.Hook>
            {
                new Response.Hook {ConsumerInputs = new Response.ConsumerInputs {AccountName = "azdocompliancyqueue"}},
                new Response.Hook {ConsumerInputs = new Response.ConsumerInputs {AccountName = "azdocompliancyqueue"}},
                new Response.Hook {ConsumerInputs = new Response.ConsumerInputs {AccountName = "azdocompliancyqueue"}},
                new Response.Hook {ConsumerInputs = new Response.ConsumerInputs {AccountName = "azdocompliancyqueue"}},
                new Response.Hook {ConsumerInputs = new Response.ConsumerInputs {AccountName = "dummy"}},
                new Response.Hook {ConsumerInputs = new Response.ConsumerInputs {AccountName = "someother"}},
            };

            orchestrationContextMock.Setup(x =>
                    x.CallActivityWithRetryAsync<IList<Response.Hook>>(nameof(GetHooksActivity),
                        It.IsAny<RetryOptions>(), null))
                .Returns(Task.FromResult((IList<Response.Hook>)hooks));

            //Act
            var fun = new DeleteServiceHookSubscriptionsOrchestrator(config);
            await fun.RunAsync(orchestrationContextMock.Object);

            //Assert
            orchestrationContextMock.Verify(
                x => x.CallActivityWithRetryAsync(nameof(DeleteServiceHookSubscriptionActivity), It.IsAny<RetryOptions>(), It.IsAny<Response.Hook>()),
                Times.Exactly(4));
        }
    }
}
