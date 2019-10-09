using System;
using System.Collections.Generic;
using AutoFixture;
using AutoFixture.AutoMoq;
using Functions.Activities;
using Functions.Model;
using Moq;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Functions.Tests.Activities
{
    public class ScanReleasePipelinesActivityTests
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
            var project = fixture.Create<Project>();
            var releasePipeline = fixture.Create<ReleaseDefinition>();
            var ciIdentifiers = fixture.Create<IList<string>>();

            // Act
            var activity = new ScanReleasePipelinesActivity(
                fixture.Create<EnvironmentConfig>(),
                client.Object,
                provider.Object);

            var result = await activity.RunAsync((project, releasePipeline, ciIdentifiers));

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
            var activity = new ScanReleasePipelinesActivity(
                fixture.Create<EnvironmentConfig>(),
                client.Object,
                provider.Object);

            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () => 
                await activity.RunAsync((null, null, null)));
            exception.Message.ShouldContainWithoutWhitespace("Value cannot be null. Parameter name: input");
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
            var releasePipeline = fixture.Create<ReleaseDefinition>();
            var ciIdentifiers = fixture.Create<IList<string>>();

            // Act
            var activity = new ScanReleasePipelinesActivity(
                fixture.Create<EnvironmentConfig>(),
                client.Object,
                provider.Object);

            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () => 
                await activity.RunAsync((null, releasePipeline, ciIdentifiers)));
            exception.Message.ShouldContainWithoutWhitespace("Value cannot be null. Parameter name: input");
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
            var project = fixture.Create<Project>();
            var ciIdentifiers = fixture.Create<IList<string>>();

            // Act
            var activity = new ScanReleasePipelinesActivity(
                fixture.Create<EnvironmentConfig>(),
                client.Object,
                provider.Object);

            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () => 
                await activity.RunAsync((project, null, ciIdentifiers)));
            exception.Message.ShouldContainWithoutWhitespace("Value cannot be null. Parameter name: input");
        }
    }
}