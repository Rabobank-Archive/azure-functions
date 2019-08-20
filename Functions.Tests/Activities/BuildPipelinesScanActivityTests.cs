using AutoFixture;
using AutoFixture.AutoMoq;
using Functions.Activities;
using Moq;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using Shouldly;
using System.Threading.Tasks;
using Xunit;
using Response = SecurePipelineScan.VstsService.Response;

namespace Functions.Tests.Activities
{
    public class BuildPipelinesScanActivityTests
    {
        [Fact]
        public async Task RunShouldReturnItemExtensionData()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var provider = new Mock<IRulesProvider>();
            provider
                .Setup(x => x.ReleaseRules(It.IsAny<IVstsRestClient>()))
                .Returns(fixture.CreateMany<IRule>())
                .Verifiable();

            var client = new Mock<IVstsRestClient>(MockBehavior.Strict);

            Response.BuildDefinition definition = fixture.Create<Response.BuildDefinition>();

            // Act
            var activity = new BuildPipelinesScanActivity(
                fixture.Create<EnvironmentConfig>(),
                client.Object,
                provider.Object);

            var result = await activity.Run(definition);

            // Assert
            result.ShouldNotBeNull();

            client.VerifyAll();
        }
    }
}