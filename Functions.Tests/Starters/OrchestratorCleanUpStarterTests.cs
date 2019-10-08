using System.Threading.Tasks;
using Functions.Completeness.Orchestrators;
using Functions.Completeness.Starters;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Timers;
using NSubstitute;
using Xunit;

namespace Functions.Tests.Completeness.Starters
{
    public class OrchestratorCleanUpStarterTests
    {
        [Fact]
        public async Task ShouldStartOrchestrator()
        {
            //Arrange
            var orchestrationClient = Substitute.For<DurableOrchestrationClientBase>();
            var timerInfo = new TimerInfo(Substitute.For<TimerSchedule>(), Substitute.For<ScheduleStatus>());

            //Act
            var function = new OrchestratorCleanUpStarter();
            await function.RunAsync(timerInfo, orchestrationClient);

            //Assert
            await orchestrationClient.Received().StartNewAsync(nameof(OrchestratorCleanUpOrchestrator), Arg.Any<object>());
        }
    }
}