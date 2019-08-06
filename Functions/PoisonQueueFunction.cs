using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Net.Http;
using System.Threading.Tasks;

namespace Functions
{
    public class PoisonQueueFunction
    {
        private readonly EnvironmentConfig _config;

        public PoisonQueueFunction(EnvironmentConfig config)
        {
            _config = config;
        }

        [FunctionName(nameof(PoisonQueueFunction))]
        public async Task Requeue(
            [HttpTrigger(AuthorizationLevel.Anonymous, Route = "poison/requeue/{queue}")]HttpRequestMessage request,
            string queue,
            ILogger log)
        {
            if (string.IsNullOrEmpty(queue)) return;

            log.LogInformation($"Requeue from: {queue}");

            var storage = CloudStorageAccount.Parse(_config.EventQueueStorageConnectionString);
            var client = storage.CreateCloudQueueClient();

            await RequeuePoisonMessages(
                client.GetQueueReference(queue),
                client.GetQueueReference($"{queue}-poison"),
                log);
        }

        private async Task RequeuePoisonMessages(CloudQueue queue, CloudQueue poison, ILogger log)
        {
            var message = await poison.GetMessageAsync();
            while (message != null)
            {
                log.LogInformation($"Requeue message with id: {message.Id}");
                await queue.AddMessageAsync(message);

                message = await poison.GetMessageAsync();
            }

            log.LogInformation($"Done");
        }
    }
}