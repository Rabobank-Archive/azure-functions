using System.Linq;
using AutoFixture;
using Functions.Activities;
using Functions.Helpers;
using Moq;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;
using Xunit;
using Response = SecurePipelineScan.VstsService.Response;
using Task = System.Threading.Tasks.Task;

namespace Functions.Tests.Activities
{
    public class CreateServiceHookSubscriptionsActivityTests
    {
        private const string AccountName = "azdocompliancyqueuedev";
        private const string AccountKey = "aG9pCg==";

        [Fact]
        public async Task NoSubscriptions_HooksCreated()
        {
            // Arrange 
            var fixture = new Fixture();
            var vstsRestClient = new Mock<IVstsRestClient>();

            // Act
            var function = new CreateServiceHookSubscriptionsActivity(
                new EnvironmentConfig { EventQueueStorageAccountName = AccountName, EventQueueStorageAccountKey = AccountKey },
                vstsRestClient.Object);

            await function.RunAsync(fixture.Create<CreateServiceHookSubscriptionsActivityRequest>());

            // Assert
            vstsRestClient
                .Verify(x => x.PostAsync(
                    It.IsAny<IVstsRequest<Hooks.Add.Body, Response.Hook>>(),
                    It.Is<Hooks.Add.Body>(b =>
                        b.EventType == "build.complete" &&
                        b.ConsumerInputs.QueueName == StorageQueueNames.BuildCompletedQueueName &&
                        b.ConsumerInputs.AccountName == AccountName &&
                        b.ConsumerInputs.AccountKey == AccountKey)));

            vstsRestClient
                .Verify(x => x.PostAsync(
                    It.IsAny<IVstsRequest<Hooks.Add.Body, Response.Hook>>(),
                    It.Is<Hooks.Add.Body>(b =>
                        b.EventType == "ms.vss-release.deployment-completed-event" &&
                        b.ConsumerInputs.QueueName == StorageQueueNames.ReleaseDeploymentCompletedQueueName &&
                        b.ConsumerInputs.AccountName == AccountName &&
                        b.ConsumerInputs.AccountKey == AccountKey)));
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
                .With(h => h.QueueName, StorageQueueNames.BuildCompletedQueueName)
                .With(h => h.AccountName, AccountName));
            fixture.Customize<Response.Hook>(ctx => ctx
                .With(h => h.EventType, "build.complete"));

            var client = new Mock<IVstsRestClient>();

            var request = new CreateServiceHookSubscriptionsActivityRequest
            {
                Project = fixture.Create<Response.Project>(),
                ExistingHooks = fixture.CreateMany<Response.Hook>().ToList()
            };
            
            // Act
            var function = new CreateServiceHookSubscriptionsActivity(
                new EnvironmentConfig { EventQueueStorageAccountName = AccountName, EventQueueStorageAccountKey = AccountKey },
                client.Object);

            await function.RunAsync(request);

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
                .With(h => h.QueueName, StorageQueueNames.ReleaseDeploymentCompletedQueueName)
                .With(h => h.AccountName, AccountName));
            fixture.Customize<Response.Hook>(ctx => ctx
                .With(h => h.EventType, "ms.vss-release.deployment-completed-event"));

            var client = new Mock<IVstsRestClient>();

            var request = new CreateServiceHookSubscriptionsActivityRequest
            {
                Project = fixture.Create<Response.Project>(),
                ExistingHooks = fixture.CreateMany<Response.Hook>().ToList()
            };
            
            // Act
            var function = new CreateServiceHookSubscriptionsActivity(
                new EnvironmentConfig { EventQueueStorageAccountName = AccountName, EventQueueStorageAccountKey = AccountKey },
                client.Object);

            await function.RunAsync(request);

            // Assert
            client.Verify();
            client.Verify(x => x.PostAsync(
                    It.IsAny<IVstsRequest<Hooks.Add.Body, Response.Hook>>(),
                    It.Is<Hooks.Add.Body>(b => b.EventType == "ms.vss-release.deployment-completed-event")),
                Times.Never());
        }
    }
}