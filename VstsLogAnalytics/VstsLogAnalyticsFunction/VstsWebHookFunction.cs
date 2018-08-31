
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Threading.Tasks;

namespace VstsWebhookFunction
{
    public static class VstsWebHookFunction
    {
        [FunctionName("VstsWebHookFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                log.LogInformation("Vsts Webhook Triggered");

                CloudBlobClient client;
                CloudBlobContainer container;

                var connectionstring = Environment.GetEnvironmentVariable("connectionString", EnvironmentVariableTarget.Process);

                var storageAccount = CloudStorageAccount.Parse(connectionstring);
                client = storageAccount.CreateCloudBlobClient();

                container = client.GetContainerReference("vstsevents");
                await container.CreateIfNotExistsAsync();

                var fileName = $"{DateTime.Now.ToString("yyyyMMddhhmmss")}-{Guid.NewGuid()}.json";

                var blob = container.GetBlockBlobReference(fileName);
                blob.Properties.ContentType = "application/json";

                await blob.UploadFromStreamAsync(req.Body);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message, ex);
            }

            return new OkObjectResult($"Vsts Trigger handled");
        }
    }
}
