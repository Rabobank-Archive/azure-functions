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
    public class GetBuildPipelinesActivity
    {
        private readonly IVstsRestClient _azuredo;

        public GetBuildPipelinesActivity(IVstsRestClient azuredo) => _azuredo = azuredo;

        [FunctionName(nameof(GetBuildPipelinesActivity))]
        public async Task<IList<Response.BuildDefinition>> RunAsync([ActivityTrigger]
            string projectId)
        {
            if (projectId == null)
                throw new ArgumentNullException(nameof(projectId));

            var buildPipelines = _azuredo.Get(Builds.BuildDefinitions(projectId))
                .ToList();

            var result = await Task.WhenAll(buildPipelines.Select(
                 b => _azuredo.GetAsync(Builds.BuildDefinition(projectId, b.Id))))
                .ConfigureAwait(false);
            return result.ToList();
        }
    }
}