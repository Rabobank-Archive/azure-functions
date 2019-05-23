using System.Threading.Tasks;
using Functions.GlobalPermissionsScan;
using Functions.Tests;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Timers;
using Microsoft.Extensions.Logging;
using Moq;
using SecurePipelineScan.VstsService;
using Response = SecurePipelineScan.VstsService.Response;
using Xunit;

namespace Functions.Tests.GlobalPermissionsScan
{
    public class SecurityScanFunctionTests
    {
        [Fact]
        public async Task RunShouldCallGetProjectsExactlyOnce()
        {
            //Arrange
            var orchestrationClientMock = new Mock<DurableOrchestrationClientBase>();
            var clientMock = new Mock<IVstsRestClient>();
            var logMock = new Mock<ILogger>();
            var timerInfoMock = CreateTimerInfoMock();

            var projects = ProjectsTestHelper.CreateMultipleProjectsResponse(1);
            
            clientMock.Setup(x => x.Get(It.IsAny<IVstsRequest<Response.Multiple<Response.Project>>>())).Returns(projects);

            //Act
            GlobalPermissionsScanFunction fun = new GlobalPermissionsScanFunction(clientMock.Object);
            await fun.Run(timerInfoMock, orchestrationClientMock.Object, logMock.Object);
            
            //Assert
            clientMock.Verify(x => x.Get(It.IsAny<IVstsRequest<Response.Multiple<Response.Project>>>()), Times.Exactly(1));

        }

        [Fact]
        public async Task RunShouldCallOrchestrationFunctionExactlyOnce()
        {
            //Arrange       
            var orchestrationClientMock = new Mock<DurableOrchestrationClientBase>();
            var clientMock = new Mock<IVstsRestClient>();
            var logMock = new Mock<ILogger>();
            var timerInfoMock = CreateTimerInfoMock();

            var projects = ProjectsTestHelper.CreateMultipleProjectsResponse(2);
            
            clientMock.Setup(x => x.Get(It.IsAny<IVstsRequest<Response.Multiple<Response.Project>>>())).Returns(projects);

            //Act
            GlobalPermissionsScanFunction fun = new GlobalPermissionsScanFunction(clientMock.Object);
            await fun.Run(timerInfoMock, orchestrationClientMock.Object, logMock.Object);
            
            //Assert
            orchestrationClientMock.Verify(x => x.StartNewAsync(nameof(GlobalPermissionsScanProjectOrchestration), It.IsAny<object>()), Times.AtLeastOnce());

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