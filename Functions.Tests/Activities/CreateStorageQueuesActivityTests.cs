using System;
using System.Threading.Tasks;
using AutoFixture;
using Functions.Activities;
using Functions.Model;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Moq;
using Xunit;

namespace Functions.Tests.Activities
{
    public class CreateStorageQueuesActivityTests
    {
        [Fact]
        public async Task ShouldCallCreateIfNotExistsForQueues()
        {
            // Arrange
            var fixture = new Fixture();
            var context = new Mock<IDurableActivityContext>();

            var cloudQueueClient =
                new Mock<CloudQueueClient>(fixture.Create<Uri>(), fixture.Create<StorageCredentials>(), null);
            var buildCompletedQueue = new Mock<CloudQueue>(fixture.Create<Uri>());
            var releaseDeploymentCompletedQueue = new Mock<CloudQueue>(fixture.Create<Uri>());

            cloudQueueClient.Setup(c => c.GetQueueReference(StorageQueueNames.BuildCompletedQueueName))
                .Returns(buildCompletedQueue.Object);
            cloudQueueClient.Setup(c => c.GetQueueReference(StorageQueueNames.ReleaseDeploymentCompletedQueueName))
                .Returns(releaseDeploymentCompletedQueue.Object);

            // Act
            var fun = new CreateStorageQueuesActivity(cloudQueueClient.Object);
            await fun.RunAsync(context.Object);

            // Assert
            buildCompletedQueue.Verify(c => c.CreateIfNotExistsAsync(), Times.Once);
            releaseDeploymentCompletedQueue.Verify(c => c.CreateIfNotExistsAsync(), Times.Once);
        }
    }
}