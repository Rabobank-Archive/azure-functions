using AutoFixture;
using Functions.Orchestrators;
using Functions.Starters;
using Microsoft.Azure.WebJobs;
using Moq;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Functions.Tests.Starters
{
    public class DeleteHooksStarterTests
    {
        [Fact]
        public async Task RunShouldCallOrchestratorExactlyOnce()
        {
            //Arrange       
            var fixture = new Fixture();
            var config = fixture.Create<EnvironmentConfig>();
            var client = new Mock<DurableOrchestrationClientBase>();

            //Act
            var fun = new DeleteHooksStarter(config);
            await fun.RunAsync("", client.Object);

            //Assert
            client.Verify(x => x.StartNewAsync(
                    nameof(DeleteHooksOrchestrator), 
                    config.EventQueueStorageAccountName),
                Times.Once());
        }
    }
}