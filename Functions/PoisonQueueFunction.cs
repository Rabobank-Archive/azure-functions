using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Functions;
using Microsoft.Azure.WebJobs.Extensions.Http;

public class PoisonQueueFunction
{
    private readonly EnvironmentConfig _config;

    public PoisonQueueFunction(EnvironmentConfig config)
    {
        _config = config;
    }

    [FunctionName(nameof(PoisonQueueFunction))]
    public async Task Requeue(
        [HttpTrigger(AuthorizationLevel.Anonymous, Route = "poison/requeue/{queue}")]HttpRequestMessage request, string queue)
    {
        if (string.IsNullOrEmpty(queue)) return;

        var storage = CloudStorageAccount.Parse(_config.StorageAccountConnectionString);
        var client = storage.CreateCloudQueueClient();

        await RequeuePoisonMessages(
            client.GetQueueReference(queue), 
        client.GetQueueReference($"{queue}-poison"));
    }
    
    public static async Task RequeuePoisonMessages(CloudQueue queue, CloudQueue poison)
    {
        var message = await poison.GetMessageAsync();
        while (message != null)
        {
            await poison.DeleteMessageAsync(message);
            await queue.AddMessageAsync(message);
            
            message = await poison.GetMessageAsync();
        }
    }
}