using AutoFixture;
using AutoFixture.AutoMoq;
using Functions.Activities;
using Moq;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using Shouldly;
using System.Threading.Tasks;
using Xunit;

namespace Functions.Tests.Activities
{
    public class RepositoryScanActivityTests
    {
        [Fact]
        public async Task RunShouldReturnItemExtensionDataForRepository()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var provider = new Mock<IRulesProvider>();
            provider
                .Setup(x => x.RepositoryRules(It.IsAny<IVstsRestClient>()))
                .Returns(fixture.CreateMany<IRule>())
                .Verifiable();

            var client = new Mock<IVstsRestClient>(MockBehavior.Strict);

            var request = fixture.Create<RepositoriesScanActivityRequest>();

            // Act
            var activity = new RepositoriesScanActivity(
                fixture.Create<EnvironmentConfig>(),
                client.Object,
                provider.Object);

            var result = await activity.RunAsync(request);

            // Assert
            result.ShouldNotBeNull();

            client.VerifyAll();
        }
    }
}