using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using Functions.Activities;
using Functions.Model;
using LogAnalytics.Client;
using NSubstitute;
using Xunit;

namespace Functions.Tests.Activities
{
    public class UploadReleaseLogActivityTests
    {
        private readonly Fixture _fixture;

        public UploadReleaseLogActivityTests()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoNSubstituteCustomization());
        }

        [Fact]
        public async Task ShouldUploadToLogAnalytics()
        {
            // Arrange
            var client = Substitute.For<ILogAnalyticsClient>();
            var config = new EnvironmentConfig { Organization = "somecompany" };
            var projectName = _fixture.Create<string>();
            var releaseId = _fixture.Create<int>();
            var releasePipelineId = _fixture.Create<string>();
            var deploymentMethods = _fixture.CreateMany<DeploymentMethod>();
            var approved = _fixture.Create<bool>();

            // Act
            var fun = new UploadReleaseLogActivity(client, config);
            await fun.RunAsync((projectName, releaseId, releasePipelineId, deploymentMethods, approved));

            // Assert
            await client.Received().AddCustomLogJsonAsync("impact_analysis_log",
                Arg.Any<ReleaseLogItem>(), "evaluatedDate");
        }
    }
}