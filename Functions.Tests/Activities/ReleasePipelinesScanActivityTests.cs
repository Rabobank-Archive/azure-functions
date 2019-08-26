using System;
using AutoFixture;
using AutoFixture.AutoMoq;
using Functions.Activities;
using Moq;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
using Shouldly;
using Xunit;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace Functions.Tests.Activities
{
    public class ReleasePipelinesScanActivityTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public ReleasePipelinesScanActivityTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

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
            var request = fixture.Create<ReleasePipelinesScanActivityRequest>();

            // Act
            var activity = new ReleasePipelinesScanActivity(
                fixture.Create<EnvironmentConfig>(),
                client.Object,
                provider.Object);

            var result = await activity.Run(request);

            // Assert
            result.ShouldNotBeNull();

            client.VerifyAll();
        }

        [Fact]
        public async Task RunWithNullRequestShouldThrowException()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var provider = new Mock<IRulesProvider>();
            provider
                .Setup(x => x.ReleaseRules(It.IsAny<IVstsRestClient>()))
                .Returns(fixture.CreateMany<IRule>())
                .Verifiable();

            var client = new Mock<IVstsRestClient>(MockBehavior.Strict);

            // Act
            var activity = new ReleasePipelinesScanActivity(
                fixture.Create<EnvironmentConfig>(),
                client.Object,
                provider.Object);

            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () => await activity.Run(null));
            exception.Message.ShouldContain("Value cannot be null.\nParameter name: request");
        }
        
        [Fact]
        public async Task RunWithNullProjectInRequestShouldThrowException()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var provider = new Mock<IRulesProvider>();
            provider
                .Setup(x => x.ReleaseRules(It.IsAny<IVstsRestClient>()))
                .Returns(fixture.CreateMany<IRule>())
                .Verifiable();

            var client = new Mock<IVstsRestClient>(MockBehavior.Strict);
            var request = new ReleasePipelinesScanActivityRequest
            {
                Project = null, ReleaseDefinition = fixture.Create<ReleaseDefinition>()
            };


            // Act
            var activity = new ReleasePipelinesScanActivity(
                fixture.Create<EnvironmentConfig>(),
                client.Object,
                provider.Object);

            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () => await activity.Run(request));
            exception.Message.ShouldContain("Value cannot be null.\nParameter name: Project");
        }
        
        [Fact]
        public async Task RunWithNullDefinitionInRequestShouldThrowException()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var provider = new Mock<IRulesProvider>();
            provider
                .Setup(x => x.ReleaseRules(It.IsAny<IVstsRestClient>()))
                .Returns(fixture.CreateMany<IRule>())
                .Verifiable();

            var client = new Mock<IVstsRestClient>(MockBehavior.Strict);
            var request = new ReleasePipelinesScanActivityRequest
            {
                Project = fixture.Create<Project>(), ReleaseDefinition = null
            };


            // Act
            var activity = new ReleasePipelinesScanActivity(
                fixture.Create<EnvironmentConfig>(),
                client.Object,
                provider.Object);

            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () => await activity.Run(request));
            exception.Message.ShouldContain("Value cannot be null.\nParameter name: ReleaseDefinition");
        }
    }
}