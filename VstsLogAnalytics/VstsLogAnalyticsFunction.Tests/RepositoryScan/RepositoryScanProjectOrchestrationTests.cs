using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Moq;
using Rules.Reports;
using SecurePipelineScan.VstsService.Response;
using VstsLogAnalyticsFunction.RepositoryScan;
using Xunit;

namespace VstsLogAnalyticsFunction.Tests.RepositoryScan
{
    public class RepositoryScanProjectOrchestrationTests
    {
        [Fact]
        public async Task RunWithHasTwoProjectsShouldCallActivityAsyncForEachProject()
        {
            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());

            //Arrange
            var durableOrchestrationContextMock = new Mock<DurableOrchestrationContextBase>();
            durableOrchestrationContextMock.Setup(context => context.GetInput<List<Project>>()).Returns(fixture.CreateMany<Project>(2).ToList());

            //Act
            await RepositoryScanProjectOrchestration.Run(durableOrchestrationContextMock.Object, new Mock<ILogger>().Object);
            
            //Assert
            durableOrchestrationContextMock.Verify(x => x.CallActivityAsync<IEnumerable<RepositoryReport>>(nameof(RepositoryScanProjectActivity), It.IsAny<Project>()),Times.Exactly(2));
        }
        
    }
}