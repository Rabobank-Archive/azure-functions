using AutoFixture;
using AutoFixture.AutoMoq;
using Functions.Activities;
using Moq;
using SecurePipelineScan.VstsService;
using Response = SecurePipelineScan.VstsService.Response;
using Shouldly;
using Xunit;
using System.Threading.Tasks;

namespace Functions.Tests.Activities
{
    public class GetBuildPipelinesActivityTests
    {
        [Fact]
        public async Task RunShouldReturnListOfBuildDefinitions()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var client = new Mock<IVstsRestClient>();

            client
                .Setup(x => x.Get(It.IsAny<IEnumerableRequest<Response.BuildDefinition>>()))
                .Returns(fixture.CreateMany<Response.BuildDefinition>());

            client
                .Setup(x => x.GetAsync(It.IsAny<IVstsRequest<Response.BuildDefinition>>()))
                .ReturnsAsync(fixture.Create<Response.BuildDefinition>());

            var projectId = fixture.Create<string>();
            var activity = new GetBuildPipelinesActivity(client.Object);
            
            // Act
            var buildPipelines = await activity.RunAsync(projectId)
                .ConfigureAwait(false);
            
            // Assert
            buildPipelines.ShouldNotBeNull();
            buildPipelines.ShouldNotBeEmpty();
        }
    }
}