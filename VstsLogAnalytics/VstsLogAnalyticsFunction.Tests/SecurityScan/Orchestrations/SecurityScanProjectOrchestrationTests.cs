using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Moq;
using SecurePipelineScan.Rules.Reports;
using SecurePipelineScan.VstsService.Response;
using Xunit;

namespace VstsLogAnalyticsFunction.Tests.SecurityScan.Orchestrations
{
    public class SecurityScanProjectOrchestrationTests
    {
        [Fact]
        public async System.Threading.Tasks.Task RunWithHasTwoProjectsShouldCallActivityAsyncForEachProject()
        {
            //Arrange
            var durableOrchestrationContextMock = new Mock<DurableOrchestrationContextBase>();
            durableOrchestrationContextMock.Setup(context => context.GetInput<List<Project>>()).Returns(ProjectsTestHelper.CreateMultipleProjectsResponse().ToList());

            //Act

            var fun = new SecurityScanProjectOrchestration();
            await fun.Run(durableOrchestrationContextMock.Object, new Mock<ILogger>().Object);
            
            //Assert
            durableOrchestrationContextMock.Verify(x => x.CallActivityAsync<IEnumerable<SecurityReport>>(nameof(SecurityScanProjectActivity), It.IsAny<Project>()), Times.Exactly(2));
        }
        
        
        
    }
}