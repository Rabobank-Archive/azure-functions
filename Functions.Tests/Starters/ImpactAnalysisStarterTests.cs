using System.Threading.Tasks;
using Functions.Orchestrators;
using Functions.Starters;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Timers;
using NSubstitute;
using Xunit;

namespace Functions.Tests.Starters
{
    public class ImpactAnalysisStarterTests
    {
        [Fact]
        public async Task ShouldStartOrchestrator()
        {
            //Arrange
            var orchestrationClient = Substitute.For<IDurableOrchestrationClient>();
            var timerInfo = new TimerInfo(Substitute.For<TimerSchedule>(), Substitute.For<ScheduleStatus>());

            //Act
            var function = new ImpactAnalysisStarter();
            await function.RunAsync(timerInfo, orchestrationClient);

            //Assert
            await orchestrationClient.Received().StartNewAsync<object>(nameof(ImpactAnalysisOrchestrator), null, null);
        }
    }
}