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
    public class ReleaseDefinitionsForProjectActivityTests
    {
        [Fact]
        public void RunShouldReturnListOfBuildDefinitions()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var client = new Mock<IVstsRestClient>();

            client
                .Setup(x => x.Get(It.IsAny<IEnumerableRequest<ReleaseDefinition>>()))
                .Returns(fixture.CreateMany<ReleaseDefinition>());
            
            var project = fixture.Create<Project>();
            var activity = new ReleaseDefinitionsForProjectActivity(client.Object);
            
            // Act
            var definitions = activity.Run(project);
            
            // Assert
            definitions.ShouldNotBeNull();
            definitions.ShouldNotBeEmpty();
        }
    }
}