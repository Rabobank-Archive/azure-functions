using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Moq;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
using VstsLogAnalyticsFunction.SecurityScan.Activites;
using VstsLogAnalyticsFunction.SecurityScan.Orchestrations;
using Xunit;

namespace VstsLogAnalyticsFunction.Tests.SecurityScan.Orchestrations
{
    public class GetAllProjectTasksTests
    {

        [Fact]
        public async Task RunShouldCallGetProject()
        {
            //Arrange
            var clientMock = new Mock<IVstsRestClient>(MockBehavior.Strict);
            var durableOrchestrationContextMock = new Mock<DurableOrchestrationContextBase>();
            var iLoggerMock = new Mock<ILogger>();

            var allProjects = CreateProjectsResponse(); 
                
            clientMock.Setup(x => x.Get(It.IsAny<IVstsRestRequest<Multiple<Project>>>())).Returns(allProjects);

            //Act
            await GetAllProjectTasks.Run(durableOrchestrationContextMock.Object, clientMock.Object, iLoggerMock.Object);
            
            //Assert
            clientMock.Verify(x => x.Get(It.IsAny<IVstsRestRequest<Multiple<Project>>>()),Times.AtLeastOnce());
        }
        
        [Fact]
        public async Task RunWithTwoProjectsShouldCallCallActivityAsyncForEachProject()
        {
            //Arrange
            var clientMock = new Mock<IVstsRestClient>(MockBehavior.Strict);
            var durableOrchestrationContextMock = new Mock<DurableOrchestrationContextBase>();
            var iLoggerMock = new Mock<ILogger>();

            var allProjects = CreateProjectsResponse(); 
                
            clientMock.Setup(x => x.Get(It.IsAny<IVstsRestRequest<Multiple<Project>>>())).Returns(allProjects);

            //Act
            await GetAllProjectTasks.Run(durableOrchestrationContextMock.Object, clientMock.Object, iLoggerMock.Object);
            
            //Assert
            durableOrchestrationContextMock.Verify(x => x.CallActivityAsync<int>(nameof(CreateSecurityReport), It.IsAny<object>()),Times.Exactly(2));

        }
        
        private static Multiple<Project> CreateProjectsResponse()
        {
            var project1 = new Project
            {
                Id = "1",
                Name = "TAS"
            };

            var project2 = new Project
            {
                Id = "2",
                Name = "TASSIE"
            };
            var allProjects = new Multiple<Project>(project1, project2);
            return allProjects;
        }
        
       
        
    }
}