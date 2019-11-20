using AutoFixture;
using AutoFixture.AutoMoq;
using Functions.Activities;
using Moq;
using SecurePipelineScan.VstsService;
using Response = SecurePipelineScan.VstsService.Response;
using Shouldly;
using Xunit;
using System.Threading.Tasks;
using Functions.Model;

namespace Functions.Tests.Activities
{
    public class GetReleasesActivityTests
    {
        [Fact]
        public async Task RunShouldReturnListOfReleases()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var client = new Mock<IVstsRestClient>();

            client
                .Setup(x => x.Get(It.IsAny<IEnumerableRequest<Response.Release>>()))
                .Returns(fixture.CreateMany<Response.Release>());

            client
                .Setup(x => x.GetAsync(It.IsAny<IVstsRequest<Response.Release>>()))
                .ReturnsAsync(fixture.Create<Response.Release>());

            var projectId = fixture.Create<string>();
            var productionItem = fixture.Create<ProductionItem>();
            var activity = new GetReleasesActivity(client.Object);
            
            // Act
            var releases = await activity.RunAsync((projectId, productionItem))
                .ConfigureAwait(false);
            
            // Assert
            releases.ShouldNotBeNull();
            releases.ShouldNotBeEmpty();
        }
    }
}