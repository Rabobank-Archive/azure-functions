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
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestMessage req,
            [OrchestrationClient] DurableOrchestrationClientBase starter)
        {
            var result = await starter.StartNewAsync(nameof(DeleteServiceHookSubscriptionsOrchestrator), null);

            return new OkObjectResult(result);
        }
    }
}
