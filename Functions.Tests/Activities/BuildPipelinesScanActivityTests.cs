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
    public class BuildPipelinesScanActivityTests
    {
        [Fact]
        public async Task RunShouldReturnItemExtensionDataForBuildDefinition()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var provider = new Mock<IRulesProvider>();
            provider
                .Setup(x => x.ReleaseRules(It.IsAny<IVstsRestClient>()))
                .Returns(fixture.CreateMany<IRule>())
                .Verifiable();

            var client = new Mock<IVstsRestClient>(MockBehavior.Strict);

            var request = fixture.Create<BuildPipelinesScanActivityRequest>();

            // Act
            var activity = new BuildPipelinesScanActivity(
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