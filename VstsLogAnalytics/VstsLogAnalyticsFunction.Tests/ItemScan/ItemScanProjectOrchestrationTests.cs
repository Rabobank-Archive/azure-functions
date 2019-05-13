using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Moq;
using SecurePipelineScan.VstsService.Response;
using VstsLogAnalyticsFunction.RepositoryScan;
using Xunit;

namespace VstsLogAnalyticsFunction.Tests.RepositoryScan
{
    public class ItemScanProjectOrchestrationTests
    {
        [Fact]
        public async System.Threading.Tasks.Task RunWithHasTwoProjectsShouldCallActivityAsyncForEachProject()
        {
            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());

            //Arrange
            var context = new Mock<DurableOrchestrationContextBase>();
            context
                .Setup(c => c.GetInput<Multiple<Project>>())
                .Returns(fixture.Create<Multiple<Project>>());

            //Act
            var target = new ItemScanProjectOrchestration();
            await target.Run(context.Object, new Mock<ILogger>().Object);
            
            //Assert
            context.Verify(x => 
                x.CallActivityAsync(ItemScanPermissionsActivity.ActivityName, It.IsAny<Project>()),
                Times.AtLeast(2));
        }        
    }
}