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
    public class DeleteHooksOrchestratorTests
    {
        private readonly Fixture _fixture;

        public DeleteHooksOrchestratorTests()
        {
            _fixture = new Fixture();
        }
        
        [Fact]
        public async Task RunShouldCallGetHooksExactlyOnce()
        {
            //Arrange
            var context = new Mock<DurableOrchestrationContextBase>();
            context.Setup(x =>
                    x.CallActivityWithRetryAsync<IList<Response.Hook>>(nameof(GetHooksActivity),
                        It.IsAny<RetryOptions>(), null))
                .Returns(Task.FromResult(_fixture.CreateMany<Response.Hook>().ToList() as IList<Response.Hook>));

            //Act
            var fun = new DeleteHooksOrchestrator();
            await fun.RunAsync(context.Object);

            //Assert
            context.Verify(
                x => x.CallActivityWithRetryAsync<IList<Response.Hook>>(nameof(GetHooksActivity),
                    It.IsAny<RetryOptions>(), null), Times.Once);
        }
        
        [Fact]
        public async Task RunShouldCallActivityExactlyOnceForCompliancyHooks()
        {
            //Arrange
            var accountName = _fixture.Create<string>();
            var context = new Mock<DurableOrchestrationContextBase>();
            context
                .Setup(x => x.GetInput<string>())
                .Returns(accountName);
            
            var hooks = new List<Response.Hook>
            {
                new Response.Hook {ConsumerInputs = new Response.ConsumerInputs { AccountName = accountName }},
                new Response.Hook {ConsumerInputs = new Response.ConsumerInputs { AccountName = accountName }},
                new Response.Hook {ConsumerInputs = new Response.ConsumerInputs { AccountName = accountName }},
                new Response.Hook {ConsumerInputs = new Response.ConsumerInputs { AccountName = accountName }},
                new Response.Hook {ConsumerInputs = new Response.ConsumerInputs { AccountName = "dummy" }},
                new Response.Hook {ConsumerInputs = new Response.ConsumerInputs { AccountName = "someother" }},
            };

            context.Setup(x =>
                    x.CallActivityWithRetryAsync<IList<Response.Hook>>(nameof(GetHooksActivity),
                        It.IsAny<RetryOptions>(), null))
                .Returns(Task.FromResult((IList<Response.Hook>)hooks));

            //Act
            var fun = new DeleteHooksOrchestrator();
            await fun.RunAsync(context.Object);

            //Assert
            context.Verify(
                x => x.CallActivityWithRetryAsync(nameof(DeleteHooksActivity), It.IsAny<RetryOptions>(), It.IsAny<Response.Hook>()),
                Times.Exactly(4));
        }
    }
}
