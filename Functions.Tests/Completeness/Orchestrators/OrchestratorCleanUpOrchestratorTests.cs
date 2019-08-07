using System.Threading.Tasks;
using Functions.Completeness.Activities;
using Functions.Completeness.Orchestrators;
using Microsoft.Azure.WebJobs;
using NSubstitute;
using Xunit;

namespace Functions.Tests.Completeness.Orchestrators
{
    public class OrchestratorCleanUpOrchestratorTests
    {
        [Fact]
        public async Task ShouldStartActivities()
        {
            //Arrange
            var orchestrationContext = Substitute.For<DurableOrchestrationContextBase>();

            //Act
            var function = new OrchestratorCleanUpOrchestrator();
            await function.RunAsync(orchestrationContext);

            //Assert
            await orchestrationContext.Received().CallActivityAsync(nameof(PurgeMultipleOrchestratorsActivity), Arg.Any<string>());
        }
    }
}