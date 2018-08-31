using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace VstsWebhookFunction
{
    //public static class VstsStorageToLogAnalyticsFunction
    //{
    //    [FunctionName("VstsStorageToLogAnalyticsFunction")]
    //    public static void Run([BlobTrigger("vstsevents/{name}", Connection = "connectionString")]Stream eventBlob, string name, ILogger log)
    //    {
    //        log.LogInformation($"Vsts blob event triggered for processing with name:{name} \n Size: {eventBlob.Length} Bytes");

    //        string requestBody = new StreamReader(eventBlob).ReadToEnd();
    //        dynamic data = JsonConvert.DeserializeObject(requestBody);

    //        var eventtype = data?.eventType;

    //    }
    //}
}
