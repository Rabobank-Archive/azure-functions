using System;
using System.Threading.Tasks;
using Functions.Activities;
using Functions.Model;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage.Queue;
using Moq;
using Unmockable;
using Xunit;

namespace Functions.Tests.Activities
{
    public class CreateStorageQueuesActivityTests
    {
        [Fact]
        public async Task ShouldCallCreateIfNotExistsForQueues()
        {
            // Arrange
            var context = new Mock<DurableActivityContextBase>();
            var cloudQueueClient = new Intercept<CloudQueueClient>();
            var buildCompletedQueue = new Mock<CloudQueue>(new Uri("http://bla.com"));
            var releaseDeploymentCompletedQueue = new Mock<CloudQueue>(new Uri("http://bla.com"));

            cloudQueueClient.Setup(c => c.GetQueueReference(StorageQueueNames.BuildCompletedQueueName))
                .Returns(buildCompletedQueue.Object);
            cloudQueueClient.Setup(c => c.GetQueueReference(StorageQueueNames.ReleaseDeploymentCompletedQueueName))
                .Returns(releaseDeploymentCompletedQueue.Object);
            
            // Act
            var fun = new CreateStorageQueuesActivity(cloudQueueClient);
            await fun.RunAsync(context.Object);

            // Assert
            buildCompletedQueue.Verify(c => c.CreateIfNotExistsAsync(), Times.Once);
            releaseDeploymentCompletedQueue.Verify(c => c.CreateIfNotExistsAsync(), Times.Once);
        }
    }
}