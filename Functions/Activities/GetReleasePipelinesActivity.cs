using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;
using Response = SecurePipelineScan.VstsService.Response;

namespace Functions.Activities
{
    public class GetReleasePipelinesActivity
    {
        private readonly IVstsRestClient _azuredo;

        public GetReleasePipelinesActivity(IVstsRestClient azuredo) => _azuredo = azuredo;

        [FunctionName(nameof(GetReleasePipelinesActivity))]
        public async Task<IList<Response.ReleaseDefinition>> RunAsync([ActivityTrigger]
            string projectId)
        {
            if (projectId == null)
                throw new ArgumentNullException(nameof(projectId));

            var releasePipelines = _azuredo.Get(ReleaseManagement.Definitions(projectId))
                .ToList();

            var result = await Task.WhenAll(releasePipelines.Select(
                r => _azuredo.GetAsync(ReleaseManagement.Definition(projectId, r.Id))))
                .ConfigureAwait(false);
            return result.ToList();
        }
    }
}