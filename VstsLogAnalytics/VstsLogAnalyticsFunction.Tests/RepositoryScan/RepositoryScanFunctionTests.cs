using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Timers;
using Microsoft.Extensions.Logging;
using Moq;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
using Xunit;

namespace VstsLogAnalyticsFunction.Tests.RepositoryScan
{
    public class RepositoryScanFunctionTests
    {
        [Fact]
        public async System.Threading.Tasks.Task GivenThereAreProjectsItShouldStartOrchestration()
        {
            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());

            //Arrange
            var orchestration = new Mock<DurableOrchestrationClientBase>();
            var azure = new Mock<IVstsRestClient>();
            azure
                .Setup(x => x.Get(It.IsAny<VstsRestRequest<Multiple<Project>>>()))
                .Returns(fixture.Create<Multiple<Project>>);    

            var logger = new Mock<ILogger>();
            var timer = CreateTimerInfoMock();

            //Act
            RepositoryScanFunction fun = new RepositoryScanFunction(azure.Object);
            await fun.Run(timer, orchestration.Object, logger.Object);

            //Assert
            orchestration.Verify(
                x => x.StartNewAsync(nameof(RepositoryScanProjectOrchestration), It.IsAny<Multiple<Project>>()), 
                Times.Once);
        }


        private static TimerInfo CreateTimerInfoMock()
        {
            return new TimerInfo(new Mock<TimerSchedule>().Object, new Mock<ScheduleStatus>().Object);
        }
    }
}