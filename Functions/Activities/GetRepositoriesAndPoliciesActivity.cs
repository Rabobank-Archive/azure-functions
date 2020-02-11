using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;
using Response = SecurePipelineScan.VstsService.Response;

namespace Functions.Activities
{
    public class GetRepositoriesAndPoliciesActivity
    {
        private readonly IVstsRestClient _azuredo;

        public GetRepositoriesAndPoliciesActivity(IVstsRestClient azuredo) => _azuredo = azuredo;

        [FunctionName(nameof(GetRepositoriesAndPoliciesActivity))]
        public IEnumerable<Response.Repository>
            Run([ActivityTrigger] Response.Project project) =>
                _azuredo.Get(Repository.Repositories(project.Id));
    }
}