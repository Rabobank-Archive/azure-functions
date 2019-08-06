using AutoFixture;
using Moq;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;
using System.Threading.Tasks;
using Xunit;
using Response = SecurePipelineScan.VstsService.Response;

namespace Functions.Tests
{
    public class ServiceHooksSubscriptionsTests
    {
        private const string StorageAccountConnectionString = "DefaultEndpointsProtocol=https;AccountName=azdocompliancyqueuedev;AccountKey=aG9pCg==;EndpointSuffix=core.windows.net";
        private const string AccountName = "azdocompliancyqueuedev";

        [Fact]
        public async Task NoSubscriptions_HooksCreated()
        {
            // Arrange 
            var fixture = new Fixture();
            var client = new Mock<IVstsRestClient>();
            client
                .Setup(x => x.Get(It.IsAny<IEnumerableRequest<Response.Project>>()))
                .Returns(fixture.CreateMany<Response.Project>())
                .Verifiable();
            client
                .Setup(x => x.Get(It.IsAny<IEnumerableRequest<Response.Hook>>()))
                .Returns(fixture.CreateMany<Response.Hook>())
                .Verifiable();

            // Act
            var function = new ServiceHooksSubscriptions(
                new EnvironmentConfig { StorageAccountConnectionString = StorageAccountConnectionString },
                client.Object);

            await function.Run(null);

            // Assert
            client
                .Verify(x => x.PostAsync(
                    It.IsAny<IVstsRequest<Hooks.Add.Body, Response.Hook>>(),
                    It.Is<Hooks.Add.Body>(b =>
                        b.ConsumerInputs.QueueName == "buildcompleted" &&
                        b.ConsumerInputs.AccountName == AccountName &&
                        b.ConsumerInputs.AccountKey == "aG9pCg==")));

            client
                .Verify(x => x.PostAsync(
                    It.IsAny<IVstsRequest<Hooks.Add.Body, Response.Hook>>(),
                    It.Is<Hooks.Add.Body>(b =>
                        b.ConsumerInputs.QueueName == "releasedeploymentcompleted" &&
                        b.ConsumerInputs.AccountName == AccountName &&
                        b.ConsumerInputs.AccountKey == "aG9pCg==")));
        }

        [Fact]
        public async Task SkipBuildCompletedHook_WhenAlreadySubscribed()
        {
            // Arrange 
            var fixture = new Fixture();
            fixture
                .Customize<Response.Project>(ctx => ctx.With(p => p.Id, "project-id"));

            fixture.Customize<Response.PublisherInputs>(ctx => ctx
                .With(h => h.ProjectId, "project-id"));
            fixture.Customize<Response.ConsumerInputs>(ctx => ctx
                .With(h => h.QueueName, "buildcompleted")
                .With(h => h.AccountName, AccountName));
            fixture.Customize<Response.Hook>(ctx => ctx
                .With(h => h.EventType, "build.complete"));

            var client = new Mock<IVstsRestClient>();
            client
                .Setup(x => x.Get(It.IsAny<IEnumerableRequest<Response.Project>>()))
                .Returns(fixture.CreateMany<Response.Project>())
                .Verifiable();
            client
                .Setup(x => x.Get(It.IsAny<IEnumerableRequest<Response.Hook>>()))
                .Returns(fixture.CreateMany<Response.Hook>())
                .Verifiable();

            // Act
            var function = new ServiceHooksSubscriptions(
                new EnvironmentConfig { StorageAccountConnectionString = StorageAccountConnectionString },
                client.Object);

            await function.Run(null);

            // Assert
            client.Verify();
            client.Verify(x => x.PostAsync(
                    It.IsAny<IVstsRequest<Hooks.Add.Body, Response.Hook>>(),
                    It.Is<Hooks.Add.Body>(b => b.EventType == "build.complete")),
                Times.Never());
        }

        [Fact]
        public async Task SkipReleaseDeploymentCompletedHook_WhenAlreadySubscribed()
        {
            // Arrange 
            var fixture = new Fixture();
            fixture
                .Customize<Response.Project>(ctx => ctx.With(p => p.Id, "project-id"));

            fixture.Customize<Response.PublisherInputs>(ctx => ctx
                .With(h => h.ProjectId, "project-id"));
            fixture.Customize<Response.ConsumerInputs>(ctx => ctx
                .With(h => h.QueueName, "releasedeploymentcompleted")
                .With(h => h.AccountName, AccountName));
            fixture.Customize<Response.Hook>(ctx => ctx
                .With(h => h.EventType, "ms.vss-release.deployment-completed-event"));

            var client = new Mock<IVstsRestClient>();
            client
                .Setup(x => x.Get(It.IsAny<IEnumerableRequest<Response.Project>>()))
                .Returns(fixture.CreateMany<Response.Project>())
                .Verifiable();
            client
                .Setup(x => x.Get(It.IsAny<IEnumerableRequest<Response.Hook>>()))
                .Returns(fixture.CreateMany<Response.Hook>())
                .Verifiable();

            // Act
            var function = new ServiceHooksSubscriptions(
                new EnvironmentConfig { StorageAccountConnectionString = StorageAccountConnectionString },
                client.Object);

            await function.Run(null);

            // Assert
            client.Verify();
            client.Verify(x => x.PostAsync(
                    It.IsAny<IVstsRequest<Hooks.Add.Body, Response.Hook>>(),
                    It.Is<Hooks.Add.Body>(b => b.EventType == "ms.vss-release.deployment-completed-event")),
                Times.Never());
        }
    }
}