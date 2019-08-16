using System.Linq;
using AutoFixture;
using Functions.Activities;
using Microsoft.Azure.WebJobs;
using Moq;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
using Shouldly;
using Xunit;

namespace Functions.Tests.Activities
{
    public class GetProjectsActivityTests
    {
        private readonly Fixture _fixture;

        public GetProjectsActivityTests()
        {
            _fixture = new Fixture();
        }
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(10)]
        public void ReturnsProjectsFromRequest(int projectCount) 
        {
            // Arrange
            var context = new Mock<DurableActivityContextBase>();
            var client = new Mock<IVstsRestClient>();
            var projects = _fixture.CreateMany<Project>(projectCount).ToList();
            client.Setup(c => c.Get(It.IsAny<IEnumerableRequest<Project>>()))
                .Returns(projects);
            
            // Act
            var fun = new GetProjectsActivity(client.Object);
            var result = fun.Run(context.Object);
            
            // Assert
            result.Union(projects).Count().ShouldBe(projectCount);
        }
    }
}