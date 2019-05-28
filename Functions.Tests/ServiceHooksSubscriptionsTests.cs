using AutoFixture;
using Moq;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;
using SecurePipelineScan.VstsService.Response;
using Xunit;
using Project = SecurePipelineScan.VstsService.Response.Project;

namespace Functions.Tests
{
    public class ServiceHooksSubscriptionsTests
    {
        private const string StorageAccountConnectionString = "DefaultEndpointsProtocol=https;AccountName=azdocompliancyqueuedev;AccountKey=aG9pCg==;EndpointSuffix=core.windows.net";
        private const string AccountName = "azdocompliancyqueuedev";

        [Fact]
        public void NoSubscriptions_HooksCreated()
        {
            // Arrange 
            var fixture = new Fixture();
            var client = new Mock<IVstsRestClient>();
            client
                .Setup(x => x.Get(It.IsAny<IVstsRequest<Multiple<Project>>>()))
                .Returns(fixture.CreateMany<Project>)
                .Verifiable();
            client
                .Setup(x => x.Get(It.IsAny<IVstsRequest<Multiple<Hook>>>()))
                .Returns(fixture.CreateMany<Hook>)
                .Verifiable();

            // Act
            var function = new ServiceHooksSubscriptions(
                new EnvironmentConfig { StorageAccountConnectionString =  StorageAccountConnectionString }, 
                client.Object);

            function.Run(null);

            // Assert
            client
                .Verify(x => x.Post(
                    It.IsAny<IVstsRequest<Hooks.Add.Body, Hook>>(), 
                    It.Is<Hooks.Add.Body>(b => 
                        b.ConsumerInputs.QueueName == "buildcompleted" &&
                        b.ConsumerInputs.AccountName == AccountName && 
                        b.ConsumerInputs.AccountKey == "aG9pCg==")));

            client
                .Verify(x => x.Post(
                    It.IsAny<IVstsRequest<Hooks.Add.Body, Hook>>(), 
                    It.Is<Hooks.Add.Body>(b => 
                        b.ConsumerInputs.QueueName == "releasedeploymentcompleted" &&
                        b.ConsumerInputs.AccountName == AccountName &&
                        b.ConsumerInputs.AccountKey == "aG9pCg==")));
        }
        
        [Fact]
        public void SkipBuildCompletedHook_WhenAlreadySubscribed()
        {
            // Arrange 
            var fixture = new Fixture();
            fixture
                .Customize<Project>(ctx => ctx.With(p => p.Id, "project-id"));
            
            fixture.Customize<PublisherInputs>(ctx => ctx
                .With(h => h.ProjectId, "project-id"));
            fixture.Customize<ConsumerInputs>(ctx => ctx
                .With(h => h.QueueName, "buildcompleted")
                .With(h => h.AccountName, AccountName));
            fixture.Customize<Hook>(ctx => ctx
                .With(h => h.EventType, "build.complete"));
            
            var client = new Mock<IVstsRestClient>();
            client
                .Setup(x => x.Get(It.IsAny<IVstsRequest<Multiple<Project>>>()))
                .Returns(fixture.CreateMany<Project>)
                .Verifiable();
            client
                .Setup(x => x.Get(It.IsAny<IVstsRequest<Multiple<Hook>>>()))
                .Returns(fixture.CreateMany<Hook>)
                .Verifiable();
            
            // Act
            var function = new ServiceHooksSubscriptions(
                new EnvironmentConfig { StorageAccountConnectionString =  StorageAccountConnectionString }, 
                client.Object);

            function.Run(null);

            // Assert
            client.Verify();
            client.Verify(x => x.Post(
                    It.IsAny<IVstsRequest<Hooks.Add.Body, Hook>>(), 
                    It.Is<Hooks.Add.Body>(b => b.EventType == "build.complete")), 
                Times.Never());
        }
        
        [Fact]
        public void SkipReleaseDeploymentCompletedHook_WhenAlreadySubscribed()
        {
            // Arrange 
            var fixture = new Fixture();
            fixture
                .Customize<Project>(ctx => ctx.With(p => p.Id, "project-id"));
            
            fixture.Customize<PublisherInputs>(ctx => ctx
                .With(h => h.ProjectId, "project-id"));
            fixture.Customize<ConsumerInputs>(ctx => ctx
                .With(h => h.QueueName, "releasedeploymentcompleted")
                .With(h => h.AccountName, AccountName));
            fixture.Customize<Hook>(ctx => ctx
                .With(h => h.EventType, "ms.vss-release.deployment-completed-event"));
            
            var client = new Mock<IVstsRestClient>();
            client
                .Setup(x => x.Get(It.IsAny<IVstsRequest<Multiple<Project>>>()))
                .Returns(fixture.CreateMany<Project>)
                .Verifiable();
            client
                .Setup(x => x.Get(It.IsAny<IVstsRequest<Multiple<Hook>>>()))
                .Returns(fixture.CreateMany<Hook>)
                .Verifiable();
            
            // Act
            var function = new ServiceHooksSubscriptions(
                new EnvironmentConfig { StorageAccountConnectionString =  StorageAccountConnectionString }, 
                client.Object);

            function.Run(null);

            // Assert
            client.Verify();
            client.Verify(x => x.Post(
                    It.IsAny<IVstsRequest<Hooks.Add.Body, Hook>>(), 
                    It.Is<Hooks.Add.Body>(b => b.EventType == "ms.vss-release.deployment-completed-event")), 
                Times.Never());
        }
    }
}