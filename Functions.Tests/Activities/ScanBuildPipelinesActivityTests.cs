using AutoFixture;
using AutoFixture.AutoMoq;
using Functions.Activities;
using SecurePipelineScan.Rules.Security;
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
            var rules = fixture.CreateMany<IBuildPipelineRule>();

            var project = fixture.Create<Response.Project>();
            var buildPipeline = fixture.Create<Response.BuildDefinition>();
            var ciIdentifiers = fixture.Create<string>();

            // Act
            var activity = new ScanBuildPipelinesActivity(
                fixture.Create<EnvironmentConfig>(),
                rules);

            var result = await activity.RunAsync((project, buildPipeline, ciIdentifiers));

            // Assert
            result.ShouldNotBeNull();
        }
    }
}