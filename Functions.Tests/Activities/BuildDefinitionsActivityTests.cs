using AutoFixture;
using AutoFixture.AutoMoq;
using Functions.Activities;
using Moq;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
using Shouldly;
using Xunit;

namespace Functions.Tests.Activities
{
    public class BuildDefinitionsActivityTests
    {

        [Fact]
        public void RunShouldReturnListOfBuildDefinitions()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var client = new Mock<IVstsRestClient>();

            client
                .Setup(x => x.Get(It.IsAny<IEnumerableRequest<BuildDefinition>>()))
                .Returns(fixture.CreateMany<BuildDefinition>());
            
            var project = fixture.Create<Project>();
            var activity = new BuildDefinitionsActivity(client.Object);
            
            // Act
            var definitions = activity.Run(project);
            
            // Assert
            definitions.ShouldNotBeNull();
            definitions.ShouldNotBeEmpty();
        }
    }
}