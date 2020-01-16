using System.Threading.Tasks;
using Functions.Model;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Functions.Activities
{
    public class CreateStorageQueuesActivity
    {
        private readonly CloudQueueClient _cloudQueueClient;

        public CreateStorageQueuesActivity(CloudQueueClient cloudQueueClient) => 
            _cloudQueueClient = cloudQueueClient;

        [FunctionName(nameof(CreateStorageQueuesActivity))]
        public async Task RunAsync([ActivityTrigger] IDurableActivityContext context)
        {
            var queue = _cloudQueueClient.GetQueueReference(StorageQueueNames.BuildCompletedQueueName);
            await queue.CreateIfNotExistsAsync().ConfigureAwait(false);
            queue = _cloudQueueClient.GetQueueReference(StorageQueueNames.ReleaseDeploymentCompletedQueueName);
            await queue.CreateIfNotExistsAsync().ConfigureAwait(false);
        }
    }
}