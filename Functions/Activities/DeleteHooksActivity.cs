using Microsoft.Azure.WebJobs;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;
using Response = SecurePipelineScan.VstsService.Response;
using System.Threading.Tasks;
using System;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Functions.Activities
{
    public class DeleteHooksActivity
    {
        private readonly IVstsRestClient _azuredo;

        public DeleteHooksActivity(IVstsRestClient azuredo) => _azuredo = azuredo;

        [FunctionName(nameof(DeleteHooksActivity))]
        public async Task RunAsync([ActivityTrigger] IDurableActivityContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var hook = context.GetInput<Response.Hook>();
            await _azuredo.DeleteAsync(Hooks.Subscription(hook.Id))
                .ConfigureAwait(false);
        }
    }
}