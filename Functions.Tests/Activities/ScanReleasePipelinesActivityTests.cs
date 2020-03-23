using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Kernel;
using AzureDevOps.Compliance.Rules;
using Functions.Activities;
using Functions.Model;
using Moq;
using SecurePipelineScan.VstsService.Response;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;
using Functions.Helpers;

namespace Functions.Tests.Activities
{
    public class ScanReleasePipelinesActivityTests
    {
        [Fact]
        public async Task EvaluatesRules_IfNoRulesApply_ThenReportShouldAlsoNotContainsRules()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var config = fixture.Create<EnvironmentConfig>();
            var rules = CreateRules(fixture, 0);
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
            // Act
            var activity = new ScanReleasePipelinesActivity(config, rules);
            var actual = await activity.RunAsync((project, pipeline));

            // Assert
            actual.ShouldNotBeNull();
            actual.Rules.ShouldNotBeNull();
            actual.Rules.ShouldBeEmpty();
        }

        [Fact]
        public async Task EvaluatesRules_IfThereAreRulesButNoStageIds_ThenTheRulesShouldStillBeProcessed()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var config = fixture.Create<EnvironmentConfig>();
            var rules = CreateRules(fixture, 3, true);
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

            // Act
            var activity = new ScanReleasePipelinesActivity(config, rules);
            var actual = await activity.RunAsync((project, pipeline));

            // Assert
            actual.ShouldNotBeNull();
            actual.Rules.ShouldNotBeNull();
            actual.Rules.ShouldNotBeEmpty();
            Assert.True(actual.Rules.All(r => r.Status.GetValueOrDefault()));
        }

        [Fact]
        public async Task RunWithNullRequestShouldThrowException()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());

            // Act
            var activity = new ScanReleasePipelinesActivity(
                fixture.Create<EnvironmentConfig>(),
                fixture.CreateMany<IReleasePipelineRule>());

            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await activity.RunAsync((null, null)));
            exception.Message.ShouldContainWithoutWhitespace("Value cannot be null. (Parameter 'input')");
        }

        [Fact]
        public async Task RunWithNullProjectInRequestShouldThrowException()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());

            var releasePipeline = fixture.Create<ReleaseDefinition>();

            // Act
            var activity = new ScanReleasePipelinesActivity(
                fixture.Create<EnvironmentConfig>(),
fixture.CreateMany<IReleasePipelineRule>());

            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await activity.RunAsync((null, releasePipeline)));
            exception.Message.ShouldContainWithoutWhitespace("Value cannot be null. (Parameter 'input')");
        }

        [Fact]
        public async Task RunWithNullDefinitionInRequestShouldThrowException()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());

            var project = fixture.Create<Project>();

            // Act
            var activity = new ScanReleasePipelinesActivity(
                fixture.Create<EnvironmentConfig>(), fixture.CreateMany<IReleasePipelineRule>());

            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await activity.RunAsync((project, null)));
            exception.Message.ShouldContainWithoutWhitespace("Value cannot be null. (Parameter 'input')");
        }

        private IEnumerable<IReleasePipelineRule> CreateRules(ISpecimenBuilder fixture, int numRules,
            params bool?[] ruleResults)
        {
            var rules = fixture.CreateMany<Mock<IReleasePipelineRule>>(numRules).ToArray();
            foreach (var rule in rules)
            {
                foreach (var ruleResult in ruleResults)
                {
                    rule.Setup(
                            r => r.EvaluateAsync(
                                It.IsAny<string>(),
                                It.IsAny<ReleaseDefinition>()))
                        .ReturnsAsync(ruleResult);
                }
            }

            return rules.Select(x => x.Object).ToArray();
        }
    }
}