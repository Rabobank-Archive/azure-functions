//using System.Collections.Generic;
//using System.Threading.Tasks;
//using AutoFixture;
//using AutoFixture.AutoMoq;
//using Functions.ItemScan;
//using Microsoft.Azure.WebJobs;
//using Microsoft.Azure.WebJobs.Extensions.Timers;
//using Microsoft.Extensions.Logging;
//using Moq;
//using SecurePipelineScan.VstsService;
//using Response = SecurePipelineScan.VstsService.Response;
//using Xunit;
//
//namespace Functions.Tests.ItemScan
//{
//    public class ItemScanFunctionTests
//    {
//        [Fact]
//        public async Task GivenThereAreProjectsItShouldStartOrchestration()
//        {
//            var fixture = new Fixture();
//            fixture.Customize(new AutoMoqCustomization());
//
//            //Arrange
//            var orchestration = new Mock<DurableOrchestrationClientBase>();
//            var azure = new Mock<IVstsRestClient>();
//            azure
//                .Setup(x => x.Get(It.IsAny<IVstsRequest<Response.Multiple<Response.Project>>>()))
//                .Returns(fixture.CreateMany<Response.Project>());    
//
//            var logger = new Mock<ILogger>();
//            var timer = CreateTimerInfoMock();
//
//            //Act
//            ItemScanFunction fun = new ItemScanFunction(azure.Object);
//            await fun.Run(timer, orchestration.Object, logger.Object);
//
//            //Assert
//            orchestration.Verify(
//                x => x.StartNewAsync(nameof(ItemScanProjectOrchestration), It.IsAny<IEnumerable<Response.Project>>()), 
//                Times.Once);
//        }
//
//
//        private static TimerInfo CreateTimerInfoMock()
//        {
//            return new TimerInfo(new Mock<TimerSchedule>().Object, new Mock<ScheduleStatus>().Object);
//        }
//    }
//}