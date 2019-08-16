using Microsoft.Azure.WebJobs;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;
using Response = SecurePipelineScan.VstsService.Response;
using System.Threading.Tasks;

namespace Functions.Activities
{
    public class DeleteServiceHookSubscriptionActivity
    {
        private readonly IVstsRestClient _azuredo;

        public DeleteServiceHookSubscriptionActivity(IVstsRestClient azuredo)
        {
            _azuredo = azuredo;
        }

        [FunctionName(nameof(DeleteServiceHookSubscriptionActivity))]
        public async Task Run([ActivityTrigger] DurableActivityContextBase context)
        {
            var hook = context.GetInput<Response.Hook>();
            await _azuredo.DeleteAsync(Hooks.Subscription(hook.Id));
        }
    }
}
