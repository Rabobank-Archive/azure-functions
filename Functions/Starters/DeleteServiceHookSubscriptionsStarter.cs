using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Functions.Orchestrators;
using System;

namespace Functions.Starters
{
    public class DeleteServiceHookSubscriptionsStarter
    {
        private readonly EnvironmentConfig _config;

        public DeleteServiceHookSubscriptionsStarter(EnvironmentConfig config)
        {
            _config = config;
        }
        
        [FunctionName(nameof(DeleteServiceHookSubscriptionsStarter))]
        [NoAutomaticTrigger]
        public async Task RunAsync(string input,
            [OrchestrationClient] DurableOrchestrationClientBase starter)
        {
            if (starter == null)
                throw new ArgumentNullException(nameof(starter));

            await starter.StartNewAsync(nameof(DeleteServiceHookSubscriptionsOrchestrator), _config.EventQueueStorageAccountName);
        }
    }
}
