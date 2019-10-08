using Xunit;
using Shouldly;
using static Functions.Helpers.OrchestrationIdHelper;
using AutoFixture;
using System.Collections.Generic;
using SecurePipelineScan.VstsService.Response;
using Functions.Model;

namespace Functions.Tests.Helpers
{
    public class LinkCiToItemHelperTests
    {
        [Fact]
        public void ReturnProductionItemsForBuildPipelines()
        {
            //Arrange
            var fixture = new Fixture();

            var projectId = fixture.Create<string>();

            fixture.Customize<Artifact>(ctx => ctx
                .With(x => x.Type, "Build"));
            fixture.Customize<Project>(ctx => ctx
                .With(x => x.Id, projectId));
            fixture.Customize<BuildDefinition>(ctx => ctx
                .With(x => x.Id, "1"));
            var releasePipelines = fixture.CreateMany<ReleaseDefinition>();
            var request = fixture.Create<ItemOrchestratorRequest>();

            //Act
            //var result = LinkCisToBuildPipelines();
            //Assert
        }
    }
}