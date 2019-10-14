using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;
using Response = SecurePipelineScan.VstsService.Response;

namespace Functions.Activities
{
    public class GetHooksActivity
    {
        private readonly IVstsRestClient _client;

        public GetHooksActivity(IVstsRestClient client) => _client = client;

        [FunctionName(nameof(GetHooksActivity))]
        public IEnumerable<Response.Hook> Run([ActivityTrigger] DurableActivityContextBase context) =>
            _client.Get(Hooks.Subscriptions()).ToList();
    }
}
