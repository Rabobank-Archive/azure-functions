using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Moq;
using Shouldly;
using System;
using System.Threading.Tasks;
using Unmockable;
using Xunit;

namespace Functions.Tests
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
            var poison = client.GetQueueReference($"some-queue-poison");

            await queue.CreateAsync();
            await queue.ClearAsync();
            await poison.CreateAsync();
            await poison.ClearAsync();

            var content = Guid.NewGuid().ToString();
            await poison.AddMessageAsync(new CloudQueueMessage(content));

            // Act
            var function = new PoisonQueueFunction(client.Wrap());
            await function.Requeue(null, "some-queue", new Mock<ILogger>().Object);

            // Assert
            poison
                .PeekMessageAsync()
                .Result
                .ShouldBeNull();

            var message = await queue.GetMessageAsync();
            message
                .ShouldNotBeNull();
            message
                .AsString
                .ShouldBe(content);
        }

        [Fact]
        public async Task SkipIfQueueNameIsEmpty()
        {
            var func = new PoisonQueueFunction(null);
            await func.Requeue(null, "", new Mock<ILogger>().Object);
        }
    }
}