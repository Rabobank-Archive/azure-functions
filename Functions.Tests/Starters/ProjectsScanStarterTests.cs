using Functions.Orchestrators;
using Functions.Starters;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Timers;
using Moq;
using SecurePipelineScan.VstsService;
using Xunit;
using Response = SecurePipelineScan.VstsService.Response;
using Task = System.Threading.Tasks.Task;

namespace Functions.Tests.Starters
{
    public class ProjectsScanStarterTests
    {
        [Fact]
        public async Task RunShouldCallGetProjectsExactlyOnce()
        {
            //Arrange
            var orchestrationClientMock = new Mock<DurableOrchestrationClientBase>();
            var clientMock = new Mock<IVstsRestClient>();
            var timerInfoMock = CreateTimerInfoMock();

            var projects = ProjectsTestHelper.CreateMultipleProjectsResponse(1);

            clientMock.Setup(x => x.Get(It.IsAny<IVstsRequest<Response.Multiple<Response.Project>>>()))
                .Returns(projects);

            //Act
            var fun = new ProjectsScanStarter(clientMock.Object);
            await fun.Run(timerInfoMock, orchestrationClientMock.Object);

            //Assert
            clientMock.Verify(x => x.Get(It.IsAny<IVstsRequest<Response.Multiple<Response.Project>>>()), Times.Exactly(1));
        }

        [Fact]
        public async Task RunShouldCallOrchestrationFunctionExactlyOnce()
        {
            //Arrange       
            var orchestrationClientMock = new Mock<DurableOrchestrationClientBase>();
            var clientMock = new Mock<IVstsRestClient>();
            var timerInfoMock = CreateTimerInfoMock();

            var projects = ProjectsTestHelper.CreateMultipleProjectsResponse(2);

            clientMock.Setup(x => x.Get(It.IsAny<IVstsRequest<Response.Multiple<Response.Project>>>()))
                .Returns(projects);

            //Act
            var fun = new ProjectsScanStarter(clientMock.Object);
            await fun.Run(timerInfoMock, orchestrationClientMock.Object);

            //Assert
            orchestrationClientMock.Verify(
                x => x.StartNewAsync(nameof(ProjectScanOrchestration), It.IsAny<object>()),
                Times.AtLeastOnce());

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