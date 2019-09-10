using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Functions.Orchestrators;

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
            await starter.StartNewAsync(nameof(DeleteServiceHookSubscriptionsOrchestrator), _config.EventQueueStorageAccountName);
        }
    }
}
