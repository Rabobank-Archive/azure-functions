using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using Functions.Activities;
using Functions.Model;
using LogAnalytics.Client;
using NSubstitute;
using Response = SecurePipelineScan.VstsService.Response;
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
            var project = _fixture.Create<Response.Project>();
            var release = _fixture.Create<Response.Release>();
            var productionItem = _fixture.Create<ProductionItem>();
            var approved = _fixture.Create<bool>();

            // Act
            var fun = new UploadReleaseLogActivity(client, config);
            await fun.RunAsync((project, release, productionItem, approved));

            // Assert
            await client.Received().AddCustomLogJsonAsync("impact_analysis_log",
                Arg.Any<ReleaseLogItem>(), "evaluatedDate");
        }
    }
}