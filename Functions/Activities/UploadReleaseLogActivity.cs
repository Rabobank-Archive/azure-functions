using System;
using System.Linq;
using System.Threading.Tasks;
using Functions.Model;
using LogAnalytics.Client;
using Microsoft.Azure.WebJobs;
using Response = SecurePipelineScan.VstsService.Response;

namespace Functions.Activities
{
    public class UploadReleaseLogActivity
    {
        private readonly ILogAnalyticsClient _client;

        public UploadReleaseLogActivity(ILogAnalyticsClient client) => _client = client;

        [FunctionName(nameof(UploadReleaseLogActivity))]
        public async Task RunAsync([ActivityTrigger](Response.Project project, 
            Response.Release release, ProductionItem productionItem, bool approved) input)
        {
            if (input.project == null || input.release == null || input.productionItem == null)
                throw new ArgumentNullException(nameof(input));

            var releaseLogItem = new ReleaseLogItem
            {
                ReleaseId = input.release.Id,
                Approved = input.approved,
                CiIdentifier = string.Join(",", input.productionItem.DeploymentInfo
                    .Select(d => d.CiIdentifier)
                    .Distinct()),
                CiName = string.Join(",", input.productionItem.DeploymentInfo
                    .Select(d => d.CiName)
                    .Distinct()),
                ReleaseLink = new Uri($"https://dev.azure.com/somecompany/{input.project.Name}" +
                    $"/_releaseProgress?_a=release-pipeline-progress&releaseId={input.release.Id}"),
                ReleasePipelineId = input.productionItem.ItemId,
                ReleaseStageId = string.Join(",", input.productionItem.DeploymentInfo
                    .Select(d => d.StageId)
                    .Distinct())
            };

            await _client.AddCustomLogJsonAsync("impact_analysis_log", releaseLogItem, "evaluatedDate")
                .ConfigureAwait(false);
        }
    }
}