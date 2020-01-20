using System.Threading.Tasks;
using Functions.Orchestrators;
using Functions.Starters;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Timers;
using Moq;
using Xunit;

namespace Functions.Tests.Starters
{
    public class ConfigurationItemsStarterTests
    {
        [Fact]
        public async Task RunShouldCallOrchestratorFunctionOnce()
        {
            //Arrange       
            var orchestrationClientMock = new Mock<IDurableOrchestrationClient>();
            var timerInfoMock = CreateTimerInfoMock();

            //Act
            var fun = new ConfigurationItemsStarter();
            await fun.RunAsync(timerInfoMock, orchestrationClientMock.Object);

            //Assert
            orchestrationClientMock.Verify(
                x => x.StartNewAsync(
                    nameof(ConfigurationItemsOrchestrator),
                    It.IsAny<string>(),
                    It.Is<object>(o => o == null)),
                Times.Once());
        }

        private static TimerInfo CreateTimerInfoMock()
        {
            var timerScheduleMock = new Mock<TimerSchedule>();
            var scheduleStatusMock = new Mock<ScheduleStatus>();
            var timerInfoMock = new TimerInfo(timerScheduleMock.Object, scheduleStatusMock.Object);
            return timerInfoMock;
        }
    }
}