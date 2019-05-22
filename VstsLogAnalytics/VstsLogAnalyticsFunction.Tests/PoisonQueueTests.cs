using System;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace VstsLogAnalyticsFunction.Tests
{
    public class PoisonQueueTests
    {
        [Fact]
        public async Task ReadFromQueue()
        {
            // Arrange 
            // Use Azure Storage Emulator on Windows or azurite to use local development server
            //   a) npm i -g azurite@2
            //   b) docker run --rm -p 10001:10001 arafato/azurite
            var storage = CloudStorageAccount.Parse("UseDevelopmentStorage=true");
            var client = storage.CreateCloudQueueClient();
            
            var queue = client.GetQueueReference("some-queue");
            var poison = client.GetQueueReference($"some-poison");

            await queue.CreateIfNotExistsAsync();
            await poison.CreateIfNotExistsAsync();

            var content = Guid.NewGuid().ToString();
            await poison.AddMessageAsync(new CloudQueueMessage(content));
            
            // Act
            await PoisonQueueFunction.RequeuePoisonMessages(queue, poison);

            // Assert
            poison
                .PeekMessageAsync()
                .Result
                .ShouldBeNull();
            
            var message = await queue.PeekMessageAsync();
            message
                .ShouldNotBeNull();
            message
                .AsString
                .ShouldBe(content);
            
            await queue.ClearAsync();
        }

        [Fact]
        public void SkipIfQueueNameIsEmpty()
        {
            var func = new PoisonQueueFunction(new EnvironmentConfig { StorageAccountConnectionString = "UseDevelopmentStorage=true" });
            func.Requeue("");
        }
    }
}