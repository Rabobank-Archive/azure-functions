using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Functions.Model;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;
using Response = SecurePipelineScan.VstsService.Response;

namespace Functions.Activities
{
    public class GetReleasesActivity
    {
        private readonly IVstsRestClient _azuredo;

        public GetReleasesActivity(IVstsRestClient azuredo) => _azuredo = azuredo;

        [FunctionName(nameof(GetReleasesActivity))]
        public async Task<IList<Response.Release>> RunAsync([ActivityTrigger] (string projectId,
            string releasePipelineId, IEnumerable<DeploymentMethod> deploymentMethods) input)
        {
            if (input.projectId == null || input.releasePipelineId == null || input.deploymentMethods == null)
                throw new ArgumentNullException(nameof(input));

            var projectId = input.projectId;
            var releasePipelineId = input.releasePipelineId;
            var releasePipelineStageIds = input.deploymentMethods
                .Select(d => d.StageId);

            var releases = _azuredo.Get(ReleaseManagement.Releases(
                projectId, releasePipelineId, "environments", "1-1-2019"));

            var productionReleases = releases
                .Where(r => r.Environments
                    .Any(e => releasePipelineStageIds.Contains(e.Id.ToString())
                        && e.Status != "notStarted" && e.Status != "rejected"));

            return await Task.WhenAll(productionReleases.Select(r => _azuredo.GetAsync(
                ReleaseManagement.Release(projectId, r.Id.ToString()))))
                .ConfigureAwait(false);
        }
    }
}