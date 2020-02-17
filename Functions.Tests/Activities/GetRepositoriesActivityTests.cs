using AutoFixture;
using AutoFixture.AutoMoq;
using Functions.Activities;
using Moq;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
using Shouldly;
using Xunit;

namespace Functions.Tests.Activities
{
    public class GetRepositoriesActivityTests
    {
        [Fact]
        public void RunShouldReturnListOfRepositoriesAndPolicies()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var client = new Mock<IVstsRestClient>();

            client
                .Setup(x => x.Get(It.IsAny<IEnumerableRequest<Repository>>()))
                .Returns(fixture.CreateMany<Repository>());

            var project = fixture.Create<Project>();
            var activity = new GetRepositoriesActivity(client.Object);

            // Act
            var repositories = activity.Run(project);

            // Assert
            repositories.ShouldNotBeNull();
            repositories.ShouldNotBeEmpty();
        }
    }
}