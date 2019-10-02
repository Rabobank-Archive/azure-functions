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
using Task = System.Threading.Tasks.Task;

namespace Functions.Tests.Activities
{
    public class ReleasePipelinesScanActivityTests
    {
        [Fact]
        public async Task EvaluatesRulesAndReturnsReport()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var provider = new Mock<IRulesProvider>();
            provider
                .Setup(x => x.ReleaseRules(It.IsAny<IVstsRestClient>()))
                .Returns(fixture.CreateMany<IReleasePipelineRule>())
                .Verifiable();

            var client = new Mock<IVstsRestClient>(MockBehavior.Strict);
            var request = fixture.Create<ReleasePipelinesScanActivityRequest>();

            // Act
            var activity = new ReleasePipelinesScanActivity(
                fixture.Create<EnvironmentConfig>(),
                client.Object,
                provider.Object);

            var result = await activity.RunAsync(request);

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
                .Returns(fixture.CreateMany<IReleasePipelineRule>())
                .Verifiable();

            var client = new Mock<IVstsRestClient>(MockBehavior.Strict);

            // Act
            var activity = new ReleasePipelinesScanActivity(
                fixture.Create<EnvironmentConfig>(),
                client.Object,
                provider.Object);

            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () => await activity.RunAsync(null));
            exception.Message.ShouldContainWithoutWhitespace("Value cannot be null. Parameter name: request");
        }
        
        [Fact]
        public async Task RunWithNullProjectInRequestShouldThrowException()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var provider = new Mock<IRulesProvider>();
            provider
                .Setup(x => x.ReleaseRules(It.IsAny<IVstsRestClient>()))
                .Returns(fixture.CreateMany<IReleasePipelineRule>())
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

            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () => await activity.RunAsync(request));
            exception.Message.ShouldContainWithoutWhitespace("Value cannot be null. Parameter name: Project");
        }

        [Fact]
        public async Task RunWithNullDefinitionInRequestShouldThrowException()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var provider = new Mock<IRulesProvider>();
            provider
                .Setup(x => x.ReleaseRules(It.IsAny<IVstsRestClient>()))
                .Returns(fixture.CreateMany<IReleasePipelineRule>())
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

            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () => await activity.RunAsync(request));
            exception.Message.ShouldContainWithoutWhitespace("Value cannot be null. Parameter name: ReleaseDefinition");
        }
    }
}