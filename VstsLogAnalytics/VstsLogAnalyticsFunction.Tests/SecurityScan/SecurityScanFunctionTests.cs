using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Timers;
using Microsoft.Extensions.Logging;
using Moq;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
using Xunit;

namespace VstsLogAnalyticsFunction.Tests.SecurityScan
{
    public class SecurityScanFunctionTests
    {
        [Fact]
        public async System.Threading.Tasks.Task RunShouldCallGetProjectsExactlyOnce()
        {
            //Arrange
            var orchestrationClientMock = new Mock<DurableOrchestrationClientBase>();
            var clientMock = new Mock<IVstsRestClient>();
            var logMock = new Mock<ILogger>();
            var timerInfoMock = CreateTimerInfoMock();

            var projects = ProjectsTestHelper.CreateMultipleProjectsResponse();
            
            clientMock.Setup(x => x.Get(It.IsAny<IVstsRestRequest<Multiple<Project>>>())).Returns(projects);

            //Act
            SecurityScanFunction fun = new SecurityScanFunction(clientMock.Object);
            await fun.Run(timerInfoMock, orchestrationClientMock.Object, logMock.Object);
            
            //Assert
            clientMock.Verify(x => x.Get(It.IsAny<IVstsRestRequest<Multiple<Project>>>()), Times.Exactly(1));

        }

        [Fact]
        public async System.Threading.Tasks.Task RunShouldCallOrchestrationFunctionExactlyOnce()
        {
            //Arrange       
            var orchestrationClientMock = new Mock<DurableOrchestrationClientBase>();
            var clientMock = new Mock<IVstsRestClient>();
            var logMock = new Mock<ILogger>();
            var timerInfoMock = CreateTimerInfoMock();

            var projects = ProjectsTestHelper.CreateMultipleProjectsResponse();
            
            clientMock.Setup(x => x.Get(It.IsAny<IVstsRestRequest<Multiple<Project>>>())).Returns(projects);

            //Act
            SecurityScanFunction fun = new SecurityScanFunction(clientMock.Object);
            await fun.Run(timerInfoMock, orchestrationClientMock.Object, logMock.Object);
            
            //Assert
            orchestrationClientMock.Verify(x => x.StartNewAsync(nameof(SecurityScanProjectOrchestration), It.IsAny<object>()), Times.AtLeastOnce());

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