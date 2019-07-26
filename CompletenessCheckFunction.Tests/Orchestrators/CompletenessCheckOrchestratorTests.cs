using CompletenessCheckFunction.Activities;
using CompletenessCheckFunction.Orchestrators;
using Microsoft.Azure.WebJobs;
using NSubstitute;
using System.Threading.Tasks;
using Xunit;

namespace CompletenessCheckFunction.Tests.Orchestrators
{
    public class CompletenessCheckOrchestratorTests
    {
        [Fact]
        public async Task ShouldStartActivity()
        {
            //Arrange
            var orchestrationContext = Substitute.For<DurableOrchestrationContextBase>();


            //Act
            var function = new CompletenessCheckOrchestrator();
            await function.Run(orchestrationContext);

            //Assert
            await orchestrationContext.Received().CallActivityAsync(nameof(ScanLogAnalyticsActivity), Arg.Any<object>());
        }
    }
}
