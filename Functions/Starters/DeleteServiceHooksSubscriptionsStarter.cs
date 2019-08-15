using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;
using Functions.Orchestrators;

namespace Functions.Starters
{
    public class DeleteServiceHooksSubscriptionsStarter
    {
        private readonly EnvironmentConfig _config;
        private readonly IVstsRestClient _client;

        public DeleteServiceHooksSubscriptionsStarter(EnvironmentConfig config, IVstsRestClient client)
        {
            _config = config;
            _client = client;
        }
        
        [FunctionName(nameof(DeleteServiceHooksSubscriptionsStarter))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestMessage req,
            [OrchestrationClient] DurableOrchestrationClientBase starter)
        {
            var subscriptionsToDelete = _client
                .Get(Hooks.Subscriptions())
                .Where(h => _config.EventQueueStorageAccountName == h.ConsumerInputs.AccountName)
                .ToList();

            await starter.StartNewAsync(nameof(DeleteServiceHooksSubscriptionsOrchestrator), subscriptionsToDelete);

            return new OkResult();
        }
    }
}
