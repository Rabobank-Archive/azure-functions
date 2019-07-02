//using System.Collections.Generic;
//using System.Linq;
//using AutoFixture;
//using AutoFixture.AutoMoq;
//using Functions.ItemScan;
//using Microsoft.Azure.WebJobs;
//using Microsoft.Extensions.Logging;
//using Moq;
//using SecurePipelineScan.VstsService.Response;
//using Xunit;
//
//namespace Functions.Tests.ItemScan
//{
//    public class ItemScanProjectOrchestrationTests
//    {
//        [Fact]
//        public async System.Threading.Tasks.Task RunWithHasTwoProjectsShouldCallActivityAsyncForEachProject()
//        {
//            var fixture = new Fixture();
//            fixture.Customize(new AutoMoqCustomization());
//
//            //Arrange
//            var context = new Mock<DurableOrchestrationContextBase>();
//            context
//                .Setup(c => c.GetInput<IList<Project>>())
//                .Returns(fixture.CreateMany<Project>().ToList());
//
//            //Act
//            var target = new ItemScanProjectOrchestration();
//            await target.Run(context.Object, new Mock<ILogger>().Object);
//            
//            //Assert
//            context.Verify(x => 
//                x.CallActivityAsync(ItemScanPermissionsActivity.ActivityNameRepos, It.IsAny<Project>()),
//                Times.AtLeast(2));
//        }        
//    }
//}