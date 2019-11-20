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
        public async Task<IList<Response.Release>> RunAsync([ActivityTrigger]
            (string projectId, ProductionItem productionItem) input)
        {
            if (input.projectId == null || input.productionItem == null)
                throw new ArgumentNullException(nameof(input));

            var projectId = input.projectId;
            var releasePipelineId = input.productionItem.ItemId;
            var releasePipelineStageIds = input.productionItem.DeploymentInfo
                .Select(d => d.StageId);

            var releases = _azuredo.Get(ReleaseManagement.Releases(projectId,
                releasePipelineId, releasePipelineStageIds, "environments", "1-1-2019"))
                .ToList();

            return await Task.WhenAll(releases.Select(r => _azuredo.GetAsync(
                ReleaseManagement.Release(projectId, r.Id.ToString()))))
                .ConfigureAwait(false);
        }
    }
}