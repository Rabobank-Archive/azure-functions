using Microsoft.Azure.WebJobs;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;
using SecurePipelineScan.VstsService.Response;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Queue;
using Unmockable;
using Project = SecurePipelineScan.VstsService.Requests.Project;
using Task = System.Threading.Tasks.Task;

namespace Functions
{
    public class ServiceHooksSubscriptions
    {
        private readonly EnvironmentConfig _config;
        private readonly IVstsRestClient _vstsRestClient;

        private static readonly string[] QueueNames = new[] {"buildcompleted", "releasedeploymentcompleted"};
        private readonly IUnmockable<CloudQueueClient> _cloudQueueClient;

        public ServiceHooksSubscriptions(EnvironmentConfig config, IVstsRestClient vstsRestClient, IUnmockable<CloudQueueClient> cloudQueueClient)
        {
            _config = config;
            _vstsRestClient = vstsRestClient;
            _cloudQueueClient = cloudQueueClient;
        }

        [FunctionName(nameof(ServiceHooksSubscriptions))]
        public async Task Run([TimerTrigger("0 0 7-19 * * *", RunOnStartup = false)]TimerInfo trigger)
        {
            await CreateQueuesIfNotExist();

            await UpdateServiceSubscriptions(_config.EventQueueStorageAccountName, _config.EventQueueStorageAccountKey);
        }

        private async Task CreateQueuesIfNotExist()
        {
            foreach (var queueName in QueueNames)
            {
                var queue = _cloudQueueClient.Execute(c => c.GetQueueReference(queueName));
                await queue.CreateIfNotExistsAsync();
            }
        }

        private async Task UpdateServiceSubscriptions(string accountName, string accountKey)
        {
            var hooks = _vstsRestClient.Get(Hooks.Subscriptions()).ToList();
            foreach (var project in _vstsRestClient.Get(Project.Projects()))
            {
                foreach (var queueName in QueueNames)
                {
                    await AddHookIfNotSubscribed(
                        Hooks.AddHookSubscription(),
                        Hooks.Add.BuildCompleted(accountName, accountKey, queueName, project.Id),
                        hooks);
                }
            }
        }

        private async Task AddHookIfNotSubscribed(IVstsRequest<Hooks.Add.Body, Hook> request, Hooks.Add.Body hook, IEnumerable<Hook> hooks)
        {
            if (!hooks.Any(h => h.EventType == hook.EventType &&
                                h.ConsumerInputs.AccountName == hook.ConsumerInputs.AccountName &&
                                h.PublisherInputs.ProjectId == hook.PublisherInputs.ProjectId))
            {
                await _vstsRestClient.PostAsync(request, hook);
            }
        }
    }
}
