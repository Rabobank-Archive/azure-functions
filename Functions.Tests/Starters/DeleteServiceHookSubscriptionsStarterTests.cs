using System.Net.Http;
using Functions.Orchestrators;
using Functions.Starters;
using Microsoft.Azure.WebJobs;
using Moq;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Functions.Tests.Starters
{
    public class DeleteServiceHookSubscriptionsStarterTests
    {
        [Fact]
        public async Task RunShouldCallOrchestratorExactlyOnce()
        {
            //Arrange       
            var orchestrationClientMock = new Mock<DurableOrchestrationClientBase>();

            //Act
            var fun = new DeleteServiceHookSubscriptionsStarter();
            await fun.RunAsync(new HttpRequestMessage(), orchestrationClientMock.Object);

            //Assert
            orchestrationClientMock.Verify(
                x => x.StartNewAsync(nameof(DeleteServiceHookSubscriptionsOrchestrator), null),
                Times.Once());
        }
    }
}