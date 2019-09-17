using System.Threading.Tasks;
using Xunit;
using Shouldly;
using Functions.Activities;
using AutoFixture;
using Response = SecurePipelineScan.VstsService.Response;
using SecurePipelineScan.VstsService;
using Moq;

namespace Functions.Tests
{
    public class GetReleaseBuildRepoLinksActivityTests
    {
        [Fact]
        public async Task ShouldReturnBuildsAndReposForReleasePipeline()
        {
            // Arrange
            var fixture = new Fixture();
            var project = fixture.Create<Response.Project>();
            fixture.Customize<ReleasePipelinesScanActivityRequest>(ctx => ctx
                .With(x => x.Project, project));
            fixture.Customize<Response.BuildDefinitionReference>(ctx => ctx
                .With(x => x.Project, project));

            var client = new Mock<IVstsRestClient>();

            client
                .Setup(x => x.GetAsync(It.IsAny<IVstsRequest<Response.ReleaseDefinition>>()))
                .ReturnsAsync(fixture.Create<Response.ReleaseDefinition>());

            var request = fixture.Create<ReleasePipelinesScanActivityRequest>();

            // Act
            var activity = new GetReleaseBuildRepoLinksActivity(client.Object);
            var releaseBuildRepoLink = await activity.RunAsync(request);

            // Assert
            releaseBuildRepoLink.ReleasePipelineId.ShouldNotBeNull();
            releaseBuildRepoLink.BuildPipelineIds.ShouldNotBeEmpty();
            releaseBuildRepoLink.RepositoryIds.ShouldNotBeEmpty();
        }

        [Fact]
        public async Task ShouldReturnEmptyListsIfBuildsAndReposInReleasePipelineAreInOtherProject()
        {
            // Arrange
            var fixture = new Fixture();
            var project = fixture.Create<Response.Project>();
            fixture.Customize<ReleasePipelinesScanActivityRequest>(ctx => ctx
                .With(x => x.Project, project));
            fixture.Customize<Response.BuildDefinitionReference>(ctx => ctx
                .With(x => x.Project, new Response.Project { Id = "OtherId" } ));

            var client = new Mock<IVstsRestClient>();

            client
                .Setup(x => x.GetAsync(It.IsAny<IVstsRequest<Response.ReleaseDefinition>>()))
                .ReturnsAsync(fixture.Create<Response.ReleaseDefinition>());

            var request = fixture.Create<ReleasePipelinesScanActivityRequest>();

            // Act
            var activity = new GetReleaseBuildRepoLinksActivity(client.Object);
            var releaseBuildRepoLink = await activity.RunAsync(request);

            // Assert
            releaseBuildRepoLink.ReleasePipelineId.ShouldNotBeNull();
            releaseBuildRepoLink.BuildPipelineIds.ShouldBeEmpty();
            releaseBuildRepoLink.RepositoryIds.ShouldBeEmpty();
        }

        [Fact]
        public async Task ShouldReturnEmptyListIfNoBuildsInReleasePipeline()
        {
            // Arrange
            var fixture = new Fixture();
            var project = fixture.Create<Response.Project>();
            fixture.Customize<ReleasePipelinesScanActivityRequest>(ctx => ctx
                .With(x => x.Project, project));
            fixture.Customize<Response.BuildDefinitionReference>(ctx => ctx
                .With(x => x.Project, project)
                .With(x => x.Definition, new Response.BuildDefinition { Id = "" } ));

            var client = new Mock<IVstsRestClient>();

            client
                .Setup(x => x.GetAsync(It.IsAny<IVstsRequest<Response.ReleaseDefinition>>()))
                .ReturnsAsync(fixture.Create<Response.ReleaseDefinition>());

            var request = fixture.Create<ReleasePipelinesScanActivityRequest>();

            // Act
            var activity = new GetReleaseBuildRepoLinksActivity(client.Object);
            var releaseBuildRepoLink = await activity.RunAsync(request);

            // Assert
            releaseBuildRepoLink.ReleasePipelineId.ShouldNotBeNull();
            releaseBuildRepoLink.BuildPipelineIds.ShouldBeEmpty();
            releaseBuildRepoLink.RepositoryIds.ShouldNotBeEmpty();
        }
    }
}