using AutoFixture;
using Functions.Orchestrators;
using Functions.Starters;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
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
            var client = new Mock<IDurableOrchestrationClient>();

            //Act
            var fun = new DeleteHooksStarter(config);
            await fun.RunAsync(string.Empty, client.Object);

            //Assert
            client.Verify(x => x.StartNewAsync<object>(
                    nameof(DeleteHooksOrchestrator)
                    , string.Empty, config.EventQueueStorageAccountName),
                Times.Once());
        }
    }
}