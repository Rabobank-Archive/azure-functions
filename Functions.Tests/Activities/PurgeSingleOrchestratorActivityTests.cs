using System.Threading.Tasks;
using NSubstitute;
using Xunit;
using Functions.Activities;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Functions.Tests.Activities
{
    public class PurgeSingleOrchestratorActivityTests
    {
        [Fact]
        public async Task ShouldSendDeleteCall()
        {
            //Arrange
            var client = Substitute.For<IDurableOrchestrationClient>();

            //Act
            var func = new PurgeSingleOrchestratorActivity();
            await func.RunAsync("instanceId", client);

            //Assert
            await client.Received().PurgeInstanceHistoryAsync("instanceId");
        }
    }
}