using System.Threading.Tasks;
using Functions.Helpers;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage.Queue;
using Unmockable;

namespace Functions.Activities
{
    public class CreateStorageQueuesActivity
    {
        private readonly IUnmockable<CloudQueueClient> _cloudQueueClient;

        public CreateStorageQueuesActivity(IUnmockable<CloudQueueClient> cloudQueueClient)
        {
            _cloudQueueClient = cloudQueueClient;
        }

        [FunctionName(nameof(CreateStorageQueuesActivity))]
        public async Task RunAsync([ActivityTrigger] DurableActivityContextBase context)
        {
            var queue = _cloudQueueClient.Execute(c => c.GetQueueReference(StorageQueueNames.BuildCompletedQueueName));
            await queue.CreateIfNotExistsAsync().ConfigureAwait(false);
            queue = _cloudQueueClient.Execute(c => c.GetQueueReference(StorageQueueNames.ReleaseDeploymentCompletedQueueName));
            await queue.CreateIfNotExistsAsync().ConfigureAwait(false);
        }
    }
}
