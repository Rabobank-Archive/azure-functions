using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;
using SecurePipelineScan.VstsService.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using Project = SecurePipelineScan.VstsService.Requests.Project;
using Task = System.Threading.Tasks.Task;

namespace Functions
{
    public class ServiceHooksSubscriptions
    {
        private readonly EnvironmentConfig _config;
        private readonly IVstsRestClient _client;

        public ServiceHooksSubscriptions(EnvironmentConfig config, IVstsRestClient client)
        {
            _config = config;
            _client = client;
        }

        [FunctionName(nameof(ServiceHooksSubscriptions))]
        public async Task Run([TimerTrigger("0 0 7-19 * * *", RunOnStartup = false)]TimerInfo trigger)
        {
            // This only works because we use the account name and account key in the connection string.
            var storage = CloudStorageAccount.Parse(_config.EventQueueStorageConnectionString);
            var key = Convert.ToBase64String(storage.Credentials.ExportKey());

            await UpdateServiceSubscriptions(storage.Credentials.AccountName, key);
        }

        private async Task UpdateServiceSubscriptions(string accountName, string accountKey)
        {
            var hooks = _client.Get(Hooks.Subscriptions()).ToList();
            foreach (var project in _client.Get(Project.Projects()))
            {
                await AddHookIfNotSubscribed(
                    Hooks.AddHookSubscription(),
                    Hooks.Add.BuildCompleted(accountName, accountKey, "buildcompleted", project.Id),
                    hooks);

                await AddHookIfNotSubscribed(
                    Hooks.AddReleaseManagementSubscription(),
                    Hooks.Add.ReleaseDeploymentCompleted(accountName, accountKey, "releasedeploymentcompleted", project.Id),
                    hooks);
            }
        }

        private async Task AddHookIfNotSubscribed(IVstsRequest<Hooks.Add.Body, Hook> request, Hooks.Add.Body hook, IEnumerable<Hook> hooks)
        {
            if (!hooks.Any(h => h.EventType == hook.EventType &&
                                h.ConsumerInputs.AccountName == hook.ConsumerInputs.AccountName &&
                                h.PublisherInputs.ProjectId == hook.PublisherInputs.ProjectId))
            {
                await _client.PostAsync(request, hook);
            }
        }
    }
}
