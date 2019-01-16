using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Timers;
using Microsoft.Extensions.Logging;
using Moq;
using Rules.Reports;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
using VstsLogAnalyticsFunction.RepositoryScan;
using Xunit;

namespace VstsLogAnalyticsFunction.Tests.RepositoryScan
{
    public class RepositoryScanFunctionTests
    {
        [Fact]
        public async Task GivenThereAreProjectsItShouldStartOrchestration()
        {
            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());

            //Arrange
            var orchestrationClientMock = new Mock<DurableOrchestrationClientBase>();
            var clientMock = new Mock<IVstsRestClient>();
            clientMock.Setup(x => x.Get<Multiple<Project>>(It.IsAny<VstsRestRequest<Multiple<Project>>>()))
                      .Returns(fixture.Create<Multiple<Project>>);

            var logMock = new Mock<ILogger>();
            var timerInfoMock = CreateTimerInfoMock();
            //Act
            await RepositoryScanFunction.Run(timerInfoMock, clientMock.Object, orchestrationClientMock.Object, logMock.Object);

            //Assert
            orchestrationClientMock.Verify(x => x.StartNewAsync(nameof(RepositoryScanProjectOrchestration),It.IsAny<Multiple<Project>>()), Times.Once);
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