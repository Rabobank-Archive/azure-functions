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
using System.Collections.Generic;

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
            var project = fixture.Create<Response.Project>();
            var buildPipeline = fixture.Create<Response.BuildDefinition>();
            var ciIdentifiers = fixture.Create<string>();

            // Act
            var activity = new ScanBuildPipelinesActivity(
                fixture.Create<EnvironmentConfig>(),
                client.Object,
                provider.Object);

            var result = await activity.RunAsync((project, buildPipeline, ciIdentifiers));

            // Assert
            result.ShouldNotBeNull();

            client.VerifyAll();
        }
    }
}