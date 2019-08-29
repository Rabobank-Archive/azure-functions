using Functions.Orchestrators;
using Functions.Starters;
using Microsoft.Azure.WebJobs;
using Moq;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Functions.Tests.Starters
{
    public class CreateServiceHookSubscriptionsStarterTests
    {
        [Fact]
        public async Task RunShouldCallOrchestratorExactlyOnce()
        {
            //Arrange       
            var orchestrationClientMock = new Mock<DurableOrchestrationClientBase>();

            //Act
            var fun = new CreateServiceHookSubscriptionsStarter();
            await fun.RunAsync(null, orchestrationClientMock.Object);

            //Assert
            orchestrationClientMock.Verify(
                x => x.StartNewAsync(nameof(CreateServiceHookSubscriptionsOrchestrator), null),
                Times.Once());
        }
    }
}