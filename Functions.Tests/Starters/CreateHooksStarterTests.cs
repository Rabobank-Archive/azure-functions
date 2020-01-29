using Functions.Orchestrators;
using Functions.Starters;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Moq;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Functions.Tests.Starters
{
    public class CreateHooksStarterTests
    {
        [Fact]
        public async Task RunShouldCallOrchestratorExactlyOnce()
        {
            //Arrange       
            var orchestrationClientMock = new Mock<IDurableOrchestrationClient>();

            //Act
            var fun = new CreateHooksStarter();
            await fun.RunAsync(null, orchestrationClientMock.Object);

            //Assert
            orchestrationClientMock.Verify(
                x => x.StartNewAsync<object>(nameof(CreateHooksOrchestrator), string.Empty, null),
                Times.Once());
        }
    }
}