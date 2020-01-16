using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Functions.Orchestrators;
using System;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Functions.Starters
{
    public class DeleteHooksStarter
    {
        private readonly EnvironmentConfig _config;

        public DeleteHooksStarter(EnvironmentConfig config) => _config = config;

        [FunctionName(nameof(DeleteHooksStarter))]
        [NoAutomaticTrigger]
        public async Task RunAsync(string input,
            [DurableClient] IDurableOrchestrationClient starter)
        {
            if (starter == null)
                throw new ArgumentNullException(nameof(starter));

            await starter.StartNewAsync(nameof(DeleteHooksOrchestrator), _config.EventQueueStorageAccountName);
        }
    }
}
