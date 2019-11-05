using System;
using System.Linq;
using AutoFixture;
using Functions.Model;
using SecurePipelineScan.VstsService.Response;
using Shouldly;
using Xunit;
using static Functions.Helpers.LinkConfigurationItemHelper;

namespace Functions.Tests.Helpers
{
    public class LinkConfigurationItemHelperTests
    {
        [Fact]
        public void ReturnProductionItemsForBuildPipelines()
        {
            //Arrange
            var fixture = SetupFixtureBuild();
            var releasePipelines = fixture.CreateMany<ReleaseDefinition>(1).ToList();
            var productionItems = fixture.CreateMany<ProductionItem>(1).ToList();
            var project = fixture.Create<Project>();

            //Act
            var result = LinkCisToBuildPipelines(releasePipelines, productionItems, project);

            //Assert
            var item = result.Single();
            item.ItemId.ShouldBe("b1");
            item.DeploymentInfo[0].CiIdentifier.ShouldBe("c1");
            item.DeploymentInfo[0].StageId.ShouldBe("s1");
            item.DeploymentInfo[1].CiIdentifier.ShouldBe("c2");
            item.DeploymentInfo[1].StageId.ShouldBe("s2");
        }

        [Fact]
        public void ReturnMultipleProductionItemsForBuildPipelines()
        {
            //Arrange
            var fixture = SetupFixtureBuild();
            fixture.Customize<BuildDefinition>(ctx => ctx
                .With(x => x.Id, fixture.Create<string>));
            var releasePipelines = fixture.CreateMany<ReleaseDefinition>(5).ToList();
            var productionItems = fixture.CreateMany<ProductionItem>(1).ToList();
            var project = fixture.Create<Project>();

            //Act
            var result = LinkCisToBuildPipelines(releasePipelines, productionItems, project);

            //Assert
            result.Count.ShouldBe(5);
        }

        [Fact]
        public void ReturnsSingleProductionItemPerBuildPipeline()
        {
            //Arrange
            var fixture = SetupFixtureBuild();
            var releasePipelines = fixture.CreateMany<ReleaseDefinition>(2).ToList();
            var productionItems = new[]
            {
                fixture.Build<ProductionItem>()
                    .With(x => x.ItemId, "r1")
                    .With(x => x.DeploymentInfo, new[]
                    {
                        new DeploymentMethod{CiIdentifier = "c1", StageId = "s1"}
                    })
                    .Create(),
                fixture.Build<ProductionItem>()
                    .With(x => x.ItemId, "r1")
                    .With(x => x.DeploymentInfo, new[]
                    {
                        new DeploymentMethod{CiIdentifier = "c2", StageId = "s2"}
                    }).Create()
            };
            var project = fixture.Create<Project>();

            //Act
            var result = LinkCisToBuildPipelines(releasePipelines, productionItems, project);

            //Assert
            var item = result.Single();
            item.ItemId.ShouldBe("b1");
            item.DeploymentInfo[0].CiIdentifier.ShouldBe("c1");
            item.DeploymentInfo[0].StageId.ShouldBe("s1");
            item.DeploymentInfo[1].CiIdentifier.ShouldBe("c2");
            item.DeploymentInfo[1].StageId.ShouldBe("s2");
        }

        [Fact]
        public void ReturnNoProductionItemsForBuildPipelinesInOtherProjects()
        {
            //Arrange
            var fixture = SetupFixtureBuild();
            var releasePipelines = fixture.CreateMany<ReleaseDefinition>(1).ToList();
            var productionItems = fixture.CreateMany<ProductionItem>(1).ToList();
            fixture.Customize<Project>(ctx => ctx
                .With(x => x.Id, "OtherProject"));
            var project = fixture.Create<Project>();

            //Act
            var result = LinkCisToBuildPipelines(releasePipelines, productionItems, project);

            //Assert
            result.ShouldBeEmpty();
        }

        [Fact]
        public void ReturnNoProductionItemsForBuildPipelinesWithoutProject()
        {
            //Arrange
            var fixture = SetupFixtureBuild();
            fixture.Customize<BuildDefinitionReference>(ctx => ctx
                .Without(x => x.Project));

            var releasePipelines = fixture.CreateMany<ReleaseDefinition>(1).ToList();
            var productionItems = fixture.CreateMany<ProductionItem>(1).ToList();
            var project = fixture.Create<Project>();

            //Act
            var result = LinkCisToBuildPipelines(releasePipelines, productionItems, project);

            //Assert
            result.ShouldBeEmpty();
        }

        [Fact]
        public void ReturnNoProductionItemsForOtherArtifacts()
        {
            //Arrange
            var fixture = SetupFixtureBuild();
            fixture.Customize<Artifact>(ctx => ctx
                .With(x => x.Type, "OtherType"));

            var releasePipelines = fixture.CreateMany<ReleaseDefinition>(1).ToList();
            var productionItems = fixture.CreateMany<ProductionItem>(1).ToList();
            var project = fixture.Create<Project>();

            //Act
            var result = LinkCisToBuildPipelines(releasePipelines, productionItems, project);

            //Assert
            result.ShouldBeEmpty();
        }

        private static Fixture SetupFixtureBuild()
        {
            var fixture = new Fixture { RepeatCount = 1 };

            fixture.Customize<Artifact>(ctx => ctx
                .With(x => x.Type, "Build"));
            fixture.Customize<BuildDefinition>(ctx => ctx
                .With(x => x.Id, "b1"));
            fixture.Customize<Project>(ctx => ctx
                .With(x => x.Id, "p1"));
            fixture.Customize<ReleaseDefinition>(ctx => ctx
                .With(x => x.Id, "r1"));

            fixture.Customize<ProductionItem>(ctx => ctx
                .With(x => x.ItemId, "r1")
                .With(x => x.DeploymentInfo, new[]
                {
                    new DeploymentMethod{CiIdentifier = "c1", StageId = "s1"},
                    new DeploymentMethod{CiIdentifier = "c2", StageId = "s2"}
                }));

            return fixture;
        }

        [Fact]
        public void ReturnProductionItemsForRepositories()
        {
            //Arrange
            var fixture = SetupFixtureRepo();
            var buildPipelines = fixture.CreateMany<BuildDefinition>(1).ToList();
            var productionItems = fixture.CreateMany<ProductionItem>(1).ToList();
            var project = fixture.Create<Project>();

            //Act
            var result = LinkCisToRepositories(buildPipelines, productionItems, project);

            //Assert
            var item = result.Single();
            item.ItemId.ShouldBe("r1");

            item.DeploymentInfo[0].CiIdentifier.ShouldBe("c1");
            item.DeploymentInfo[0].StageId.ShouldBe("s1");
            item.DeploymentInfo[1].CiIdentifier.ShouldBe("c2");
            item.DeploymentInfo[1].StageId.ShouldBe("s2");
        }

        [Fact]
        public void ReturnMultipleProductionItemsForRepositories()
        {
            //Arrange
            var fixture = SetupFixtureRepo();
            fixture.Customize<Repository>(ctx => ctx
                .With(x => x.Id, fixture.Create<string>)
                .With(x => x.Url, new Uri("https://dev.azure.somecompany/p1/repo")));

            var buildPipelines = fixture.CreateMany<BuildDefinition>(5).ToList();
            var productionItems = fixture.CreateMany<ProductionItem>(1).ToList();
            var project = fixture.Create<Project>();

            //Act
            var result = LinkCisToRepositories(buildPipelines, productionItems, project);

            //Assert
            result.Count.ShouldBe(5);
        }

        [Fact]
        public void ReturnsSingleProductionItemPerRepository()
        {
            //Arrange
            var fixture = SetupFixtureRepo();
            var buildPipelines = fixture.CreateMany<BuildDefinition>(2).ToList();
            var productionItems = new[]
            {
                fixture.Build<ProductionItem>()
                    .With(x => x.ItemId, "b1")
                    .With(x => x.DeploymentInfo, new[] {new DeploymentMethod{CiIdentifier = "c1", StageId = "s1"}})
                    .Create(),
                fixture.Build<ProductionItem>()
                    .With(x => x.ItemId, "b1")
                    .With(x => x.DeploymentInfo, new[] {new DeploymentMethod{CiIdentifier = "c2", StageId = "s2"}})
                    .Create()
            };
            var project = fixture.Create<Project>();

            //Act
            var result = LinkCisToRepositories(buildPipelines, productionItems, project);

            //Assert
            var item = result.Single();
            item.ItemId.ShouldBe("r1");
            item.DeploymentInfo[0].CiIdentifier.ShouldBe("c1");
            item.DeploymentInfo[0].StageId.ShouldBe("s1");
            item.DeploymentInfo[1].CiIdentifier.ShouldBe("c2");
            item.DeploymentInfo[1].StageId.ShouldBe("s2");
        }

        [Fact]
        public void ReturnNoProductionItemsForRepositoriesInOtherProjects()
        {
            //Arrange
            var fixture = SetupFixtureRepo();
            var buildPipelines = fixture.CreateMany<BuildDefinition>(1).ToList();
            var productionItems = fixture.CreateMany<ProductionItem>(1).ToList();
            fixture.Customize<Project>(ctx => ctx
                .With(x => x.Name, "OtherProject"));
            var project = fixture.Create<Project>();

            //Act
            var result = LinkCisToRepositories(buildPipelines, productionItems, project);

            //Assert
            result.ShouldBeEmpty();
        }

        [Fact]
        public void ReturnNoProductionItemsForBuildPipelinesWithoutRepository()
        {
            //Arrange
            var fixture = SetupFixtureRepo();
            fixture.Customize<BuildDefinition>(ctx => ctx
                .With(x => x.Id, "b1")
                .Without(x => x.Repository));

            var buildPipelines = fixture.CreateMany<BuildDefinition>(1).ToList();
            var productionItems = fixture.CreateMany<ProductionItem>(1).ToList();
            var project = fixture.Create<Project>();

            //Act
            var result = LinkCisToRepositories(buildPipelines, productionItems, project);

            //Assert
            result.ShouldBeEmpty();
        }

        [Fact]
        public void ReturnDistinctCiIdentifiers()
        {
            //Arrange
            var fixture = SetupFixtureBuild();
            var productionItems = new[]
            {
                fixture.Build<ProductionItem>()
                    .With(x => x.ItemId, "r1")
                    .With(x => x.DeploymentInfo, new[]
                    {
                        new DeploymentMethod{CiIdentifier = "c1", StageId = "s1"}
                    })
                    .Create(),
                fixture.Build<ProductionItem>()
                    .With(x => x.ItemId, "r1")
                    .With(x => x.DeploymentInfo, new[]
                    {
                        new DeploymentMethod{CiIdentifier = "c1", StageId = "s2"},
                        new DeploymentMethod{CiIdentifier = "c2", StageId = "s3"}
                    }).Create()
            };

            //Act
            var result = GetCiIdentifiers(productionItems);

            //Assert
            result.ShouldBe("c1,c2");
        }

        private static Fixture SetupFixtureRepo()
        {
            var fixture = new Fixture { RepeatCount = 1 };

            fixture.Customize<BuildDefinition>(ctx => ctx
                .With(x => x.Id, "b1"));
            fixture.Customize<Repository>(ctx => ctx
                .With(x => x.Id, "r1")
                .With(x => x.Url, new Uri("https://dev.azure.somecompany/p1/repo")));
            fixture.Customize<Project>(ctx => ctx
                .With(x => x.Name, "p1"));

            fixture.Customize<ProductionItem>(ctx => ctx
                .With(x => x.ItemId, "b1")
                .With(x => x.DeploymentInfo, new[]
                {
                    new DeploymentMethod{CiIdentifier = "c1", StageId = "s1"},
                    new DeploymentMethod{CiIdentifier = "c2", StageId = "s2"}
                }));

            return fixture;
        }
    }
}