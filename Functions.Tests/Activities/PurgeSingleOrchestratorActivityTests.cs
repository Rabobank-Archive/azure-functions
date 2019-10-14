using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using NSubstitute;
using Xunit;
using Functions.Activities;

namespace Functions.Tests.Activities
{
    public class PurgeSingleOrchestratorActivityTests
    {
        [Fact]
        public async Task ShouldSendDeleteCall()
        {
            //Arrange
            var client = Substitute.For<DurableOrchestrationClientBase>();

            //Act
            var func = new PurgeSingleOrchestratorActivity();
            await func.RunAsync("instanceId", client);

            //Assert
            await client.Received().PurgeInstanceHistoryAsync("instanceId");
        }
    }
}