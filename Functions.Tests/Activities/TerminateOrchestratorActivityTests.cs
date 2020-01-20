using System.Threading.Tasks;
using NSubstitute;
using Xunit;
using Functions.Activities;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Functions.Tests.Activities
{
    public class TerminateOrchestratorActivityTests
    {
        [Fact]
        public async Task ShouldSendTerminateCall()
        {
            //Arrange
            var client = Substitute.For<IDurableOrchestrationClient>();

            //Act
            var func = new TerminateOrchestratorActivity();
            await func.RunAsync("instanceId", client);

            //Assert
            await client.Received().TerminateAsync("instanceId", Arg.Any<string>());
        }
    }
}