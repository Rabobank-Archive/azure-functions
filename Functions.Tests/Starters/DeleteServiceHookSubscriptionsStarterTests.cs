using System.Collections.Generic;
using System.Net.Http;
using Functions.Orchestrators;
using Functions.Starters;
using Microsoft.Azure.WebJobs;
using Moq;
using SecurePipelineScan.VstsService;
using Xunit;
using Response = SecurePipelineScan.VstsService.Response;
using Task = System.Threading.Tasks.Task;

namespace Functions.Tests.Starters
{
    public class DeleteServiceHookSubscriptionsStarterTests
    {
        [Fact]
        public async Task RunShouldCallGetHooksExactlyOnce()
        {
            //Arrange
            var orchestrationClientMock = new Mock<DurableOrchestrationClientBase>();
            var clientMock = new Mock<IVstsRestClient>();
            var config = new EnvironmentConfig
            {
                EventQueueStorageConnectionString = "DefaultEndpointsProtocol=https;AccountName=azdocompliancyqueue;AccountKey=ZHVtbXlrZXk=;EndpointSuffix=core.windows.net"
            };

            var hooks = new List<Response.Hook>();

            clientMock.Setup(x => x.Get(It.IsAny<IEnumerableRequest<Response.Hook>>()))
                .Returns(hooks);

            //Act
            var fun = new DeleteServiceHooksSubscriptionsStarter(config, clientMock.Object);
            await fun.Run(new HttpRequestMessage(), orchestrationClientMock.Object);

            //Assert
            clientMock.Verify(x => x.Get(It.IsAny<IEnumerableRequest<Response.Hook>>()), Times.Exactly(1));
        }

        [Fact]
        public async Task RunShouldCallOrchestratorExactlyOnceForCompliancyHooks()
        {
            //Arrange       
            var orchestrationClientMock = new Mock<DurableOrchestrationClientBase>();
            var clientMock = new Mock<IVstsRestClient>();
            var config = new EnvironmentConfig
            {
                EventQueueStorageConnectionString = "DefaultEndpointsProtocol=https;AccountName=azdocompliancyqueue;AccountKey=ZHVtbXlrZXk=;EndpointSuffix=core.windows.net"
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

            clientMock.Setup(x => x.Get(It.IsAny<IEnumerableRequest<Response.Hook>>()))
                .Returns(hooks);

            //Act
            var fun = new DeleteServiceHooksSubscriptionsStarter(config, clientMock.Object);
            await fun.Run(new HttpRequestMessage(), orchestrationClientMock.Object);

            //Assert
            orchestrationClientMock.Verify(
                x => x.StartNewAsync(nameof(DeleteServiceHooksSubscriptionsOrchestrator), It.Is<List<Response.Hook>>(r => r.Count == 4)),
                Times.Once());
        }
    }
}