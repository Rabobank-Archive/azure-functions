using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Functions.Activities;
using Functions.Model;
using Microsoft.Azure.WebJobs;

namespace Functions.Orchestrators
{
    public class ConfigurationItemsOrchestrator
    {
        [FunctionName(nameof(ConfigurationItemsOrchestrator))]
        public async Task RunAsync([OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var configurationItems =
                await context.CallActivityAsync<List<ConfigurationItem>>(
                    nameof(GetConfigurationItemsFromTableStorageActivity), null);

            await context.CallActivityAsync(nameof(UploadConfigurationItemLogsActivity), configurationItems);
        }
    }
}