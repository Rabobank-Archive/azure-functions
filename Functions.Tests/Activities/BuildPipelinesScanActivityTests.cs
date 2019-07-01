using AutoFixture;
using AutoFixture.AutoMoq;
using Functions.Activities;
using Moq;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Functions.Tests.Activities
{
    public class BuildPipelinesScanActivityTests
    {
        [Fact]
        public async Task EvaluatesRulesAndReturnsReport()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var provider = new Mock<IRulesProvider>();
            provider
                .Setup(x => x.ReleaseRules(It.IsAny<IVstsRestClient>()))
                .Returns(fixture.CreateMany<IRule>())
                .Verifiable();
            
            var client = new Mock<IVstsRestClient>(MockBehavior.Strict);
            client
                .Setup(x => x.Get(It.IsAny<IVstsRequest<Multiple<BuildDefinition>>>()))
                .Returns(fixture.CreateMany<BuildDefinition>());
            
            client
                .Setup(x => x.GetAsync(It.IsAny<IVstsRequest<ProjectProperties>>()))
                .ReturnsAsync(fixture.Create<ProjectProperties>())
                .Verifiable();
            
            // Act
            var activity = new BuildPipelinesScanActivity(
                fixture.Create<EnvironmentConfig>(), 
                client.Object,
                provider.Object);
            
            var result = await activity.Run(fixture.Create<string>());
            
            // Assert
            result.RescanUrl.ShouldNotBeNull();
            result.Reports.ShouldNotBeEmpty();
            
            client.VerifyAll();
        }
    }
}