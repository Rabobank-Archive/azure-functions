using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Functions.Model;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;
using Response = SecurePipelineScan.VstsService.Response;

namespace Functions.Activities
{
    public class CreateHooksActivity
    {
        private readonly IVstsRestClient _vstsRestClient;
        private readonly EnvironmentConfig _config;

        public CreateHooksActivity(EnvironmentConfig config, 
            IVstsRestClient vstsRestClient)
        {
            _vstsRestClient = vstsRestClient;
            _config = config;
        }

        [FunctionName(nameof(CreateHooksActivity))]
        public async Task RunAsync([ActivityTrigger] (IList<Response.Hook>, Response.Project) input)
        {
            if (input.Item1 == null || input.Item2 == null)
                throw new ArgumentNullException(nameof(input));

            var existingHooks = input.Item1;
            var project = input.Item2;

            await AddHookIfNotSubscribedAsync(
                Hooks.AddHookSubscription(),
                Hooks.Add.BuildCompleted(_config.EventQueueStorageAccountName, 
                    _config.EventQueueStorageAccountKey, StorageQueueNames.BuildCompletedQueueName,
                    project.Id), existingHooks)
                .ConfigureAwait(false);
                
            await AddHookIfNotSubscribedAsync(
                Hooks.AddReleaseManagementSubscription(),
                Hooks.Add.ReleaseDeploymentCompleted(_config.EventQueueStorageAccountName,
                    _config.EventQueueStorageAccountKey, StorageQueueNames.ReleaseDeploymentCompletedQueueName,
                    project.Id), existingHooks)
                .ConfigureAwait(false);
        }

        private async Task AddHookIfNotSubscribedAsync(IVstsRequest<Hooks.Add.Body, Response.Hook> request, 
            Hooks.Add.Body hook, IEnumerable<Response.Hook> hooks)
        {
            if (!hooks.Any(h => h.EventType == hook.EventType &&
                                h.ConsumerInputs.AccountName == hook.ConsumerInputs.AccountName &&
                                h.PublisherInputs.ProjectId == hook.PublisherInputs.ProjectId))
                await _vstsRestClient.PostAsync(request, hook).ConfigureAwait(false);
        }
    }
}