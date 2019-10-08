using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;
using Response = SecurePipelineScan.VstsService.Response;

namespace Functions.Activities
{
    public class GetProjectsActivity
    {
        private readonly IVstsRestClient _client;

        public GetProjectsActivity(IVstsRestClient client)
        {
            _client = client;
        }

        [FunctionName(nameof(GetProjectsActivity))]
        public IEnumerable<Response.Project> Run([ActivityTrigger] DurableActivityContextBase context)
        {
            return _client.Get(Project.Projects()).ToList();
        }
    }
}