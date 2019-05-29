using System;
using System.Collections.Generic;
using System.Linq;
using Functions;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;
using SecurePipelineScan.VstsService.Response;
using Project = SecurePipelineScan.VstsService.Requests.Project;

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
    public void Run([TimerTrigger("0 0 7-19 * * *", RunOnStartup=false)]TimerInfo trigger)
    {
        // This only works because we use the account name and account key in the connection string.
        var storage = CloudStorageAccount.Parse(_config.StorageAccountConnectionString);
        var key = Convert.ToBase64String(storage.Credentials.ExportKey());

        UpdateServiceSubscriptions(storage.Credentials.AccountName, key);
    }

    private void UpdateServiceSubscriptions(string accountName, string accountKey)
    {
        var hooks = _client.Get(Hooks.Subscriptions()).ToList();
        foreach (var project in _client.Get(Project.Projects()))
        {
            AddHookIfNotSubscribed(
                Hooks.AddHookSubscription(), 
                Hooks.Add.BuildCompleted(accountName, accountKey,"buildcompleted", project.Id),
                hooks);

            AddHookIfNotSubscribed(
                Hooks.AddReleaseManagementSubscription(),
                Hooks.Add.ReleaseDeploymentCompleted(accountName, accountKey, "releasedeploymentcompleted", project.Id),
                hooks);
        }
    }

    private void AddHookIfNotSubscribed(IVstsRequest<Hooks.Add.Body, Hook> request, Hooks.Add.Body hook, IEnumerable<Hook> hooks)
    {
        if (!hooks.Any(h => h.EventType == hook.EventType &&
                            h.ConsumerInputs.AccountName == hook.ConsumerInputs.AccountName &&
                            h.PublisherInputs.ProjectId == hook.PublisherInputs.ProjectId))
        {
            _client.Post(request, hook);    
        }
    }
}
