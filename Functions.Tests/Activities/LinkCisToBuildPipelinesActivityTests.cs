using System.Threading.Tasks;
using Xunit;
using Shouldly;
using Functions.Activities;
using AutoFixture;
using Response = SecurePipelineScan.VstsService.Response;
using System.Collections.Generic;

namespace Functions.Tests.Activities
{
    public class LinkCisToBuildPipelinesActivityTests
    {
        [Fact]
        public async Task ShouldReturnProductionItemsForReleasePipeline()
        {
            // Arrange
            var fixture = new Fixture();

            var projectId = fixture.Create<string>();
            var ciIdentifiers = fixture.Create<List<string>>();

            fixture.Customize<Response.Artifact>(ctx => ctx
                .With(x => x.Type, "Build"));
            fixture.Customize<Response.Project>(ctx => ctx
                .With(x => x.Id, projectId));
            fixture.Customize<Response.BuildDefinition>(ctx => ctx
                .With(x => x.Id, "1"));
            var releasePipeline = fixture.Create<Response.ReleaseDefinition>();

            // Act
            var activity = new LinkCisToBuildPipelinesActivity();
            var productionItems = activity.Run((releasePipeline, ciIdentifiers, projectId));

            // Assert
            productionItems[0].ItemId.ShouldBe("1");
            productionItems[0].CiIdentifiers.ShouldBe(ciIdentifiers);
        }

        [Fact]
        public async Task ShouldReturnEmptyListsIfBuildsAreInOtherProject()
        {
            // Arrange
            var fixture = new Fixture();

            var projectId = fixture.Create<string>();
            var ciIdentifiers = fixture.Create<List<string>>();

            fixture.Customize<Response.Artifact>(ctx => ctx
                .With(x => x.Type, "Build"));
            fixture.Customize<Response.Project>(ctx => ctx
                .With(x => x.Id, "otherId"));
            fixture.Customize<Response.BuildDefinition>(ctx => ctx
                .With(x => x.Id, "1"));
            var releasePipeline = fixture.Create<Response.ReleaseDefinition>();

            // Act
            var activity = new LinkCisToBuildPipelinesActivity();
            var productionItems = activity.Run((releasePipeline, ciIdentifiers, projectId));

            // Assert
            productionItems.ShouldBeEmpty();
        }

        [Fact]
        public async Task ShouldReturnEmptyListIfNoBuildsInReleasePipeline()
        {
            // Arrange
            var fixture = new Fixture();

            var projectId = fixture.Create<string>();
            var ciIdentifiers = fixture.Create<List<string>>();

            fixture.Customize<Response.Artifact>(ctx => ctx
                .With(x => x.Type, "Build")
                .Without(x => x.DefinitionReference));
            fixture.Customize<Response.Project>(ctx => ctx
                .With(x => x.Id, "otherId"));
            var releasePipeline = fixture.Create<Response.ReleaseDefinition>();

            // Act
            var activity = new LinkCisToBuildPipelinesActivity();
            var productionItems = activity.Run((releasePipeline, ciIdentifiers, projectId));

            // Assert
            productionItems.ShouldBeEmpty();
        }
    }
}