using AutoFixture;
using Functions.Activities;
using Moq;
using SecurePipelineScan.VstsService;
using Response = SecurePipelineScan.VstsService.Response;
using Shouldly;
using Xunit;
using System.Threading.Tasks;
using Functions.Model;
using AutoFixture.AutoNSubstitute;

namespace Functions.Tests.Activities
{
    public class GetReleasesActivityTests
    {
        private readonly Fixture _fixture;

        public GetReleasesActivityTests()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoNSubstituteCustomization());
        }

        [Fact]
        public async Task RunShouldReturnListOfReleasesForProductionStageRuns()
        {
            // Arrange
            _fixture.Customize<DeploymentMethod>(x => x
                .With(d => d.StageId, "1"));
            _fixture.Customize<Response.Environment>(x => x
                .With(e => e.Id, 1)
                .With(e => e.Status, "completed"));

            var client = new Mock<IVstsRestClient>();
            client
                .Setup(x => x.Get(It.IsAny<IEnumerableRequest<Response.Release>>()))
                .Returns(_fixture.CreateMany<Response.Release>());

            client
                .Setup(x => x.GetAsync(It.IsAny<IVstsRequest<Response.Release>>()))
                .ReturnsAsync(_fixture.Create<Response.Release>());

            var projectId = _fixture.Create<string>();
            var releasePipelineId = _fixture.Create<string>();
            var deploymentMethods = _fixture.CreateMany<DeploymentMethod>();
            var activity = new GetReleasesActivity(client.Object);

            // Act
            var releases = await activity.RunAsync((projectId, releasePipelineId, deploymentMethods))
                .ConfigureAwait(false);

            // Assert
            releases.ShouldNotBeNull();
            releases.ShouldNotBeEmpty();
        }

        [Fact]
        public async Task RunShouldReturnEmptyListForNonProductionStageRuns()
        {
            // Arrange
            _fixture.Customize<DeploymentMethod>(x => x
                .With(d => d.StageId, "2"));
            _fixture.Customize<Response.Environment>(x => x
                .With(e => e.Id, 1)
                .With(e => e.Status, "completed"));

            var client = new Mock<IVstsRestClient>();
            client
                .Setup(x => x.Get(It.IsAny<IEnumerableRequest<Response.Release>>()))
                .Returns(_fixture.CreateMany<Response.Release>());

            client
                .Setup(x => x.GetAsync(It.IsAny<IVstsRequest<Response.Release>>()))
                .ReturnsAsync(_fixture.Create<Response.Release>());

            var projectId = _fixture.Create<string>();
            var releasePipelineId = _fixture.Create<string>();
            var deploymentMethods = _fixture.CreateMany<DeploymentMethod>();
            var activity = new GetReleasesActivity(client.Object);

            // Act
            var releases = await activity.RunAsync((projectId, releasePipelineId, deploymentMethods))
                .ConfigureAwait(false);

            // Assert
            releases.ShouldNotBeNull();
            releases.ShouldBeEmpty();
        }

        [Fact]
        public async Task RunShouldReturnEmptyListForNotStartedProductionStageRuns()
        {
            // Arrange
            _fixture.Customize<DeploymentMethod>(x => x
                .With(d => d.StageId, "1"));
            _fixture.Customize<Response.Environment>(x => x
                .With(e => e.Id, 1)
                .With(e => e.Status, "notStarted"));

            var client = new Mock<IVstsRestClient>();
            client
                .Setup(x => x.Get(It.IsAny<IEnumerableRequest<Response.Release>>()))
                .Returns(_fixture.CreateMany<Response.Release>());

            client
                .Setup(x => x.GetAsync(It.IsAny<IVstsRequest<Response.Release>>()))
                .ReturnsAsync(_fixture.Create<Response.Release>());

            var projectId = _fixture.Create<string>();
            var releasePipelineId = _fixture.Create<string>();
            var deploymentMethods = _fixture.CreateMany<DeploymentMethod>();
            var activity = new GetReleasesActivity(client.Object);

            // Act
            var releases = await activity.RunAsync((projectId, releasePipelineId, deploymentMethods))
                .ConfigureAwait(false);

            // Assert
            releases.ShouldNotBeNull();
            releases.ShouldBeEmpty();
        }
    }
}