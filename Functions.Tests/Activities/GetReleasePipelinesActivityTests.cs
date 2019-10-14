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
    public class GetReleasePipelinesActivityTests
    {
        [Fact]
        public async Task RunShouldReturnListOfReleaseDefinitions()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var client = new Mock<IVstsRestClient>();

            client
                .Setup(x => x.Get(It.IsAny<IEnumerableRequest<Response.ReleaseDefinition>>()))
                .Returns(fixture.CreateMany<Response.ReleaseDefinition>());

            client
                .Setup(x => x.GetAsync(It.IsAny<IVstsRequest<Response.ReleaseDefinition>>()))
                .ReturnsAsync(fixture.Create<Response.ReleaseDefinition>());

            var projectId = fixture.Create<string>();
            var activity = new GetReleasePipelinesActivity(client.Object);
            
            // Act
            var releasePipelines = await activity.RunAsync(projectId)
                .ConfigureAwait(false);
            
            // Assert
            releasePipelines.ShouldNotBeNull();
            releasePipelines.ShouldNotBeEmpty();
        }
    }
}