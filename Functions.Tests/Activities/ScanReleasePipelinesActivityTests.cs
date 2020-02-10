using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Kernel;
using Functions.Activities;
using Functions.Model;
using Moq;
using Functions.Cmdb.Client;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;
using SecurePipelineScan.Rules.Security;

namespace Functions.Tests.Activities
{
    public class ScanReleasePipelinesActivityTests
    {
        [Theory]
        [InlineData(true, true, true, true)]
        [InlineData(false, false, false, false)]
        [InlineData(true, false, true, false)]
        public async Task EvaluatesRules_ShouldOnlyBeTrueIfAllStagesAreCompliant(
            bool? stage0RuleResult,
            bool? stage1RuleResult,
            bool? stage2RuleResult,
            bool? expected)
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var config = fixture.Create<EnvironmentConfig>();
            var provider = CreateRulesProvider(fixture, 3,
                (stageId: "0", ruleResult: stage0RuleResult),
                (stageId: "1", ruleResult: stage1RuleResult),
                (stageId: "2", ruleResult: stage2RuleResult));
            var client = new Mock<IVstsRestClient>(MockBehavior.Strict);
            var project = fixture.Create<Project>();
            var pipeline = fixture.Create<ReleaseDefinition>();
            var deploymentMethods = fixture.CreateMany<DeploymentMethod>(3).ToList();
            for (var i = 0; i < deploymentMethods.Count; i++)
            {
                var deploymentMethod = deploymentMethods[i];
                deploymentMethod.Organization = config.Organization;
                deploymentMethod.ProjectId = project.Id;
                deploymentMethod.PipelineId = pipeline.Id;
                deploymentMethod.StageId = i.ToString();
            }
            var productionItems = fixture.CreateMany<ProductionItem>(1).ToList();
            productionItems[0].DeploymentInfo = deploymentMethods;
            productionItems[0].ItemId = pipeline.Id;

            var cmdbClient = new Mock<ICmdbClient>();

            // Act
            var activity = new ScanReleasePipelinesActivity(config, client.Object, provider, cmdbClient.Object);
            var actual = await activity.RunAsync((project, pipeline, productionItems));

            // Assert
            actual.ShouldNotBeNull();
            actual.Rules.ShouldNotBeNull();
            actual.Rules.ShouldNotBeEmpty();
            client.VerifyAll();
            Assert.Equal(expected, actual.Rules.All(r => r.Status.GetValueOrDefault()));
            actual.Environments.ShouldNotBeNull();
            actual.Environments.First().Id.ShouldBe(pipeline.Environments.First().Id);
        }

        [Fact]
        public async Task EvaluatesRules_IfNoRulesApply_ThenReportShouldAlsoNotContainsRules()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var config = fixture.Create<EnvironmentConfig>();
            var provider = CreateRulesProvider(fixture, 0);
            var client = new Mock<IVstsRestClient>(MockBehavior.Strict);
            var project = fixture.Create<Project>();
            var pipeline = fixture.Create<ReleaseDefinition>();
            var deploymentMethods = fixture.CreateMany<DeploymentMethod>(3).ToList();
            for (var i = 0; i < deploymentMethods.Count; i++)
            {
                var deploymentMethod = deploymentMethods[i];
                deploymentMethod.Organization = config.Organization;
                deploymentMethod.ProjectId = project.Id;
                deploymentMethod.PipelineId = pipeline.Id;
                deploymentMethod.StageId = i.ToString();
            }
            var productionItems = fixture.CreateMany<ProductionItem>(1).ToList();
            productionItems[0].DeploymentInfo = deploymentMethods;
            productionItems[0].ItemId = pipeline.Id;

            var cmdbClient = new Mock<ICmdbClient>();

            // Act
            var activity = new ScanReleasePipelinesActivity(config, client.Object, provider, cmdbClient.Object);
            var actual = await activity.RunAsync((project, pipeline, productionItems));

            // Assert
            actual.ShouldNotBeNull();
            client.VerifyAll();
            actual.Rules.ShouldNotBeNull();
            actual.Rules.ShouldBeEmpty();
        }

        [Fact]
        public async Task EvaluatesRules_IfThereAreRulesButNoStageIds_ThenTheRulesShouldStillBeProcessed()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var config = fixture.Create<EnvironmentConfig>();
            var provider = CreateRulesProvider(fixture, 3, (stageId: null, ruleResult: true));
            var client = new Mock<IVstsRestClient>(MockBehavior.Strict);
            var project = fixture.Create<Project>();
            var pipeline = fixture.Create<ReleaseDefinition>();
            var deploymentMethods = fixture.CreateMany<DeploymentMethod>(3).ToList();
            foreach (var deploymentMethod in deploymentMethods)
            {
                deploymentMethod.Organization = config.Organization;
                deploymentMethod.ProjectId = project.Id;
                deploymentMethod.PipelineId = pipeline.Id;
                deploymentMethod.StageId = null;
            }
            var productionItems = fixture.CreateMany<ProductionItem>(1).ToList();
            productionItems[0].DeploymentInfo = deploymentMethods;
            productionItems[0].ItemId = pipeline.Id;

            var cmdbClient = new Mock<ICmdbClient>();

            // Act
            var activity = new ScanReleasePipelinesActivity(config, client.Object, provider, cmdbClient.Object);
            var actual = await activity.RunAsync((project, pipeline, productionItems));

            // Assert
            actual.ShouldNotBeNull();
            client.VerifyAll();
            actual.Rules.ShouldNotBeNull();
            actual.Rules.ShouldNotBeEmpty();
            Assert.True(actual.Rules.All(r => r.Status.GetValueOrDefault()));
        }

        [Fact]
        public async Task EvaluatesRules_IfForAnyOfTheStagesRuleEvaluatesToNull_ThenTheRulesStatusShouldAlsoBeNull()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var config = fixture.Create<EnvironmentConfig>();
            var provider = CreateRulesProvider(fixture, 3,
                (stageId: "0", ruleResult: true),
                // one of the checks returns null because the result could not be determined
                (stageId: "1", ruleResult: null),
                (stageId: "2", ruleResult: true));
            var client = new Mock<IVstsRestClient>(MockBehavior.Strict);
            var project = fixture.Create<Project>();
            var pipeline = fixture.Create<ReleaseDefinition>();
            var deploymentMethods = fixture.CreateMany<DeploymentMethod>(3).ToList();
            for (var i = 0; i < deploymentMethods.Count; i++)
            {
                var deploymentMethod = deploymentMethods[i];
                deploymentMethod.Organization = config.Organization;
                deploymentMethod.ProjectId = project.Id;
                deploymentMethod.PipelineId = pipeline.Id;
                deploymentMethod.StageId = i.ToString();
            }
            var productionItems = fixture.CreateMany<ProductionItem>(1).ToList();
            productionItems[0].DeploymentInfo = deploymentMethods;
            productionItems[0].ItemId = pipeline.Id;

            var cmdbClient = new Mock<ICmdbClient>();

            // Act
            var activity = new ScanReleasePipelinesActivity(config, client.Object, provider, cmdbClient.Object);
            var actual = await activity.RunAsync((project, pipeline, productionItems));

            // Assert
            actual.ShouldNotBeNull();
            client.VerifyAll();
            actual.Rules.ShouldNotBeNull();
            actual.Rules.ShouldNotBeEmpty();
            Assert.All(actual.Rules, r =>
            {
                Assert.Null(r.Status);
            });
        }

        [Fact]
        public async Task RunWithNullRequestShouldThrowException()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var provider = new Mock<IRulesProvider>();
            provider
                .Setup(x => x.ReleaseRules(It.IsAny<IVstsRestClient>(), null))
                .Returns(fixture.CreateMany<IReleasePipelineRule>())
                .Verifiable();

            var client = new Mock<IVstsRestClient>(MockBehavior.Strict);

            var cmdbClient = new Mock<ICmdbClient>();

            // Act
            var activity = new ScanReleasePipelinesActivity(
                fixture.Create<EnvironmentConfig>(),
                client.Object,
                provider.Object,
                cmdbClient.Object);

            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await activity.RunAsync((null, null, null)));
            exception.Message.ShouldContainWithoutWhitespace("Value cannot be null. (Parameter 'input')");
        }

        [Fact]
        public async Task RunWithNullProjectInRequestShouldThrowException()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var provider = new Mock<IRulesProvider>();
            provider
                .Setup(x => x.ReleaseRules(It.IsAny<IVstsRestClient>(), null))
                .Returns(fixture.CreateMany<IReleasePipelineRule>())
                .Verifiable();

            var client = new Mock<IVstsRestClient>(MockBehavior.Strict);
            var releasePipeline = fixture.Create<ReleaseDefinition>();
            var productionItems = fixture.Create<IList<ProductionItem>>();

            var cmdbClient = new Mock<ICmdbClient>();

            // Act
            var activity = new ScanReleasePipelinesActivity(
                fixture.Create<EnvironmentConfig>(),
                client.Object,
                provider.Object,
                cmdbClient.Object);

            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await activity.RunAsync((null, releasePipeline, productionItems)));
            exception.Message.ShouldContainWithoutWhitespace("Value cannot be null. (Parameter 'input')");
        }

        [Fact]
        public async Task RunWithNullDefinitionInRequestShouldThrowException()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var provider = new Mock<IRulesProvider>();
            provider
                .Setup(x => x.ReleaseRules(It.IsAny<IVstsRestClient>(), null))
                .Returns(fixture.CreateMany<IReleasePipelineRule>())
                .Verifiable();

            var client = new Mock<IVstsRestClient>(MockBehavior.Strict);
            var project = fixture.Create<Project>();
            var productionItems = fixture.Create<IList<ProductionItem>>();

            var cmdbClient = new Mock<ICmdbClient>();

            // Act
            var activity = new ScanReleasePipelinesActivity(
                fixture.Create<EnvironmentConfig>(),
                client.Object,
                provider.Object,
                cmdbClient.Object);

            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await activity.RunAsync((project, null, productionItems)));
            exception.Message.ShouldContainWithoutWhitespace("Value cannot be null. (Parameter 'input')");
        }

        private IRulesProvider CreateRulesProvider(ISpecimenBuilder fixture, int numRules,
            params (string stageId, bool? ruleResult)[] ruleResults)
        {
            var provider = new Mock<IRulesProvider>();
            var rules = fixture.CreateMany<Mock<IReleasePipelineRule>>(numRules).ToArray();
            foreach (var rule in rules)
            {
                foreach (var (stageId, ruleResult) in ruleResults)
                {
                    rule.Setup(
                            r => r.EvaluateAsync(
                                It.IsAny<string>(),
                                It.Is<string>(s => s == stageId),
                                It.IsAny<ReleaseDefinition>()))
                        .ReturnsAsync(ruleResult);
                }
            }

            provider
                .Setup(x => x.ReleaseRules(It.IsAny<IVstsRestClient>(), null))
                .Returns(rules.Select(r => r.Object))
                .Verifiable();
            return provider.Object;
        }
    }
}