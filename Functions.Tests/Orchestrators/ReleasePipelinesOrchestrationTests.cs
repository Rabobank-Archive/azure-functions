using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AzDoCompliancy.CustomStatus;
using Functions.Activities;
using Functions.Model;
using Functions.Orchestrators;
using Microsoft.Azure.WebJobs;
using Moq;
using SecurePipelineScan.VstsService.Response;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Functions.Tests.Orchestrators
{
    public class ReleasePipelinesOrchestrationTests
    {
        private readonly Fixture _fixture;

        public ReleasePipelinesOrchestrationTests()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoNSubstituteCustomization());
        }

        [Fact]
        public async Task ShouldCallActivityAsyncForProject()
        {
            //Arrange
            _fixture.Customize<ReleaseDefinition>(f => f
                 .With(r => r.Id, "1"));
            _fixture.Customize<ProductionItem>(f => f
                .With(p => p.ItemId, "1"));

            var mocks = new MockRepository(MockBehavior.Strict);

            var starter = mocks.Create<DurableOrchestrationContextBase>();
            starter
                .Setup(x => x.GetInput<ItemOrchestratorRequest>())
                .Returns(_fixture.Create<ItemOrchestratorRequest>());

            starter
                .Setup(x => x.InstanceId)
                .Returns(_fixture.Create<string>());

            starter
                .Setup(x => x.SetCustomStatus(It.Is<ScanOrchestrationStatus>(s => 
                    s.Scope == RuleScopes.ReleasePipelines)))
                .Verifiable();

            starter
                .Setup(x => x.CallActivityWithRetryAsync<ItemExtensionData>(
                    nameof(ReleasePipelinesScanActivity), It.IsAny<RetryOptions>(),
                    It.IsAny<ReleasePipelinesScanActivityRequest>()))
                .ReturnsAsync(_fixture.Create<ItemExtensionData>())
                .Verifiable();

            starter
                .Setup(x => x.CallActivityWithRetryAsync<List<ReleaseDefinition>>(
                    nameof(ReleaseDefinitionsForProjectActivity), It.IsAny<RetryOptions>(),
                    It.IsAny<Project>()))
                .ReturnsAsync(_fixture.CreateMany<ReleaseDefinition>().ToList())
                .Verifiable();

            starter
                .Setup(x => x.CurrentUtcDateTime).Returns(new DateTime());

            starter
                .Setup(x => x.CallActivityAsync(nameof(ExtensionDataUploadActivity),
                    It.Is<(ItemsExtensionData data, string scope)>(t => 
                    t.scope == RuleScopes.ReleasePipelines)))
                .Returns(Task.CompletedTask);

            starter
                .Setup(x => x.CallActivityAsync(nameof(LogAnalyticsUploadActivity),
                    It.Is<LogAnalyticsUploadActivityRequest>(l =>
                    l.PreventiveLogItems.All(p => p.Scope == RuleScopes.ReleasePipelines))))
                .Returns(Task.CompletedTask);

            starter
                .Setup(x => x.CallActivityWithRetryAsync<ReleaseBuildsReposLink>(
                    nameof(GetReleaseBuildRepoLinksActivity), It.IsAny<RetryOptions>(),
                    It.IsAny<ReleasePipelinesScanActivityRequest>()))
                .ReturnsAsync(_fixture.Create<ReleaseBuildsReposLink>())
                .Verifiable();

            var environmentConfig = _fixture.Create<EnvironmentConfig>();

            //Act
            var function = new ReleasePipelinesOrchestration(environmentConfig);
            await function.RunAsync(starter.Object);

            //Assert           
            mocks.VerifyAll();
        }

        [Fact]
        public void ShouldCreateRequestsWithCiIdentifiersOfMultipleReleasePipelines()
        {
            //Arrange
            var releaseBuildRepoLinks = new List<ReleaseBuildsReposLink>
            {
                new ReleaseBuildsReposLink
                {
                    ReleasePipelineId = "rel1",
                    BuildPipelineIds = new List<string> { "b1", "b2" },
                    RepositoryIds = new List<string> { "rep1" }
                },
                new ReleaseBuildsReposLink
                {
                    ReleasePipelineId = "rel2",
                    BuildPipelineIds = new List<string> { "b1", "b2" },
                    RepositoryIds = new List<string> { "rep1" }
                }
            };
            var request = new ItemOrchestratorRequest
            {
                Project = new Project(),
                ProductionItems = new List<ProductionItem>
                {
                    new ProductionItem { ItemId = "rel1", CiIdentifiers = new List<string>() { "c1", "c2" } },
                    new ProductionItem { ItemId = "rel2", CiIdentifiers = new List<string>() { "c3", "c4" } },
                }
            };

            //Act
            var function = new ReleasePipelinesOrchestration(
                new Fixture().Create<EnvironmentConfig>());
            var (buildRequest, repoRequest) = 
                function.CreateItemOrchestratorRequests(releaseBuildRepoLinks, request);

            //Assert
            buildRequest.ProductionItems.Count.ShouldBe(2);
            buildRequest.ProductionItems[0].CiIdentifiers.Count.ShouldBe(4);
            repoRequest.ProductionItems.Count.ShouldBe(1);
            repoRequest.ProductionItems[0].CiIdentifiers.Count.ShouldBe(4);
        }

        [Fact]
        public void ShouldCreateRequestsWithoutCiIdentifiersOfOtherReleasePipelines()
        {
            //Arrange
            var releaseBuildRepoLinks = new List<ReleaseBuildsReposLink>
            {
                new ReleaseBuildsReposLink
                {
                    ReleasePipelineId = "rel1",
                    BuildPipelineIds = new List<string> { "b1" },
                    RepositoryIds = new List<string> { "rep1" }
                }
            };
            var request = new ItemOrchestratorRequest
            {
                Project = new Project(),
                ProductionItems = new List<ProductionItem>
                {
                    new ProductionItem { ItemId = "otherRelease", CiIdentifiers = new List<string>() { "c1" } },
                    new ProductionItem { ItemId = "rel1", CiIdentifiers = new List<string>() { "c2" } },
                }
            };

            //Act
            var function = new ReleasePipelinesOrchestration(
                new Fixture().Create<EnvironmentConfig>());
            var (buildRequest, repoRequest) =
                function.CreateItemOrchestratorRequests(releaseBuildRepoLinks, request);

            //Assert
            buildRequest.ProductionItems[0].CiIdentifiers.ShouldContain("c2");
            buildRequest.ProductionItems[0].CiIdentifiers.ShouldNotContain("c1");
            repoRequest.ProductionItems[0].CiIdentifiers.ShouldContain("c2");
            repoRequest.ProductionItems[0].CiIdentifiers.ShouldNotContain("c1");
        }

        [Fact]
        public void ShouldCreateRequestsWithoutDoubleCiIdentifiers()
        {
            //Arrange
            var releaseBuildRepoLinks = new List<ReleaseBuildsReposLink>
            {
                new ReleaseBuildsReposLink
                {
                    ReleasePipelineId = "rel1",
                    BuildPipelineIds = new List<string> { "b1" },
                    RepositoryIds = new List<string> { "rep1" }
                },
                new ReleaseBuildsReposLink
                {
                    ReleasePipelineId = "rel2",
                    BuildPipelineIds = new List<string> { "b1" },
                    RepositoryIds = new List<string> { "rep1" }
                }
            };
            var request = new ItemOrchestratorRequest
            {
                Project = new Project(),
                ProductionItems = new List<ProductionItem>
                    {
                        new ProductionItem { ItemId = "rel1", CiIdentifiers = new List<string>() { "c1", "c2" } },
                        new ProductionItem { ItemId = "rel2", CiIdentifiers = new List<string>() { "c1", "c2" } },
                    }
            };

            //Act
            var function = new ReleasePipelinesOrchestration(
                new Fixture().Create<EnvironmentConfig>());
            var(buildRequest, repoRequest) = 
                    function.CreateItemOrchestratorRequests(releaseBuildRepoLinks, request);

            //Assert
            buildRequest.ProductionItems[0].CiIdentifiers.Count.ShouldBe(2);
            repoRequest.ProductionItems[0].CiIdentifiers.Count.ShouldBe(2);
        }
    }
}