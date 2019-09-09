using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Functions.Orchestrators;

namespace Functions.Starters
{
    public class DeleteServiceHookSubscriptionsStarter
    {
        [FunctionName(nameof(DeleteServiceHookSubscriptionsStarter))]
        [NoAutomaticTrigger]
        public async Task RunAsync(
            [OrchestrationClient] DurableOrchestrationClientBase starter)
        {
            await starter.StartNewAsync(nameof(DeleteServiceHookSubscriptionsOrchestrator), null);
        }
    }
}
