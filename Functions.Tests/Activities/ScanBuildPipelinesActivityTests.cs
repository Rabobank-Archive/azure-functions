using AutoFixture;
using AutoFixture.AutoMoq;
using Functions.Activities;
using Moq;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using Response = SecurePipelineScan.VstsService.Response;
using Shouldly;
using System.Threading.Tasks;
using Xunit;

namespace Functions.Tests.Activities
{
    public class ScanBuildPipelinesActivityTests
    {
        [Fact]
        public async Task RunShouldReturnItemExtensionDataForBuildDefinition()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var provider = new Mock<IRulesProvider>();
            provider
                .Setup(x => x.BuildRules(It.IsAny<IVstsRestClient>()))
                .Returns(fixture.CreateMany<IBuildPipelineRule>())
                .Verifiable();

            var client = new Mock<IVstsRestClient>(MockBehavior.Strict);
            var request = fixture.Create<BuildPipelinesScanActivityRequest>();

            // Act
            var activity = new ScanBuildPipelinesActivity(
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