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
        [HttpTrigger(AuthorizationLevel.Anonymous)]HttpRequestMessage request, 
        string queueName)
    {
        if (string.IsNullOrEmpty(queueName)) return;

        var storage = CloudStorageAccount.Parse(_config.StorageAccountConnectionString);
        var client = storage.CreateCloudQueueClient();
            
        var queue = client.GetQueueReference(queueName);
        var poison = client.GetQueueReference($"{queueName}-poison");

        await RequeuePoisonMessages(queue, poison);
    }
    
    public static async Task RequeuePoisonMessages(CloudQueue queue, CloudQueue poison)
    {
        var message = await poison.GetMessageAsync();
        while (message != null)
        {
            await queue.AddMessageAsync(message);
            message = await poison.GetMessageAsync();
        }
    }
}