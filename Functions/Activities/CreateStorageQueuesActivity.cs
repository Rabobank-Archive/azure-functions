using System.Threading.Tasks;
using Functions.Model;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Functions.Activities
{
    public class CreateStorageQueuesActivity
    {
        private readonly CloudQueueClient _cloudQueueClient;

        public CreateStorageQueuesActivity(CloudQueueClient cloudQueueClient) => 
            _cloudQueueClient = cloudQueueClient;

        [FunctionName(nameof(CreateStorageQueuesActivity))]
        public async Task RunAsync([ActivityTrigger] DurableActivityContextBase context)
        {
            var queue = _cloudQueueClient.GetQueueReference(StorageQueueNames.BuildCompletedQueueName);
            await queue.CreateIfNotExistsAsync().ConfigureAwait(false);
            queue = _cloudQueueClient.GetQueueReference(StorageQueueNames.ReleaseDeploymentCompletedQueueName);
            await queue.CreateIfNotExistsAsync().ConfigureAwait(false);
        }
    }
}