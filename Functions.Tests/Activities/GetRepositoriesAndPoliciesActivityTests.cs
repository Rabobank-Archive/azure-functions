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
    public class GetRepositoriesAndPoliciesActivityTests
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
            client
                .Setup(x => x.Get(It.IsAny<IEnumerableRequest<MinimumNumberOfReviewersPolicy>>()))
                .Returns(fixture.CreateMany<MinimumNumberOfReviewersPolicy>());

            var project = fixture.Create<Project>();
            var activity = new GetRepositoriesAndPoliciesActivity(client.Object);
            
            // Act
            var (repositories, policies) = activity.Run(project);
            
            // Assert
            repositories.ShouldNotBeNull();
            repositories.ShouldNotBeEmpty();
            policies.ShouldNotBeNull();
            policies.ShouldNotBeEmpty();
        }
    }
}