using CompletenessCheckFunction.Orchestrators;
using CompletenessCheckFunction.Starters;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Timers;
using NSubstitute;
using System.Threading.Tasks;
using Xunit;

namespace CompletenessCheckFunction.Tests.Starters
{
    public class CompletenessCheckStarterTests
    {
        [Fact]
        public async Task ShouldStartOrchestrator()
        {
            //Arrange
            var orchestrationClient = Substitute.For<DurableOrchestrationClientBase>();
            var timerInfo = new TimerInfo(Substitute.For<TimerSchedule>(), Substitute.For<ScheduleStatus>());

            //Act
            var function = new CompletenessCheckStarter();
            await function.Run(timerInfo, orchestrationClient);

            //Assert
            await orchestrationClient.Received().StartNewAsync(nameof(CompletenessCheckOrchestrator), Arg.Any<object>());
        }
    }
}
