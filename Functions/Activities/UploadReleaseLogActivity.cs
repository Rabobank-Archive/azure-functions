using System;
using System.Collections.Generic;
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
        private readonly EnvironmentConfig _config;

        public UploadReleaseLogActivity(ILogAnalyticsClient client, EnvironmentConfig config)
        {
            _client = client;
            _config = config;
        }

        [FunctionName(nameof(UploadReleaseLogActivity))]
        public async Task RunAsync([ActivityTrigger](Response.Project project, 
            Response.Release release, ProductionItem productionItem, bool approved) input)
        {
            if (input.project == null || input.release == null || input.productionItem == null)
                throw new ArgumentOutOfRangeException(nameof(input));

            var releaseLogItem = new ReleaseLogItem
            {
                ReleaseId = input.release.Id,
                Approved = input.approved,
                CiIdentifier = ToCommaSeparatedString(input.productionItem.DeploymentInfo, d => d.CiIdentifier),
                CiName = ToCommaSeparatedString(input.productionItem.DeploymentInfo, d => d.CiName),
                ReleaseLink = new Uri($"https://dev.azure.com/{_config.Organization}/{input.project.Name}" +
                    $"/_releaseProgress?_a=release-pipeline-progress&releaseId={input.release.Id}"),
                ReleasePipelineId = input.productionItem.ItemId,
                ReleaseStageId = ToCommaSeparatedString(input.productionItem.DeploymentInfo, d => d.StageId)
            };

            await _client.AddCustomLogJsonAsync("impact_analysis_log", releaseLogItem, "evaluatedDate")
                .ConfigureAwait(false);
        }

        private static string ToCommaSeparatedString(IEnumerable<DeploymentMethod> deploymentInfo, 
            Func<DeploymentMethod, string> p)
        {
            return string.Join(",", deploymentInfo
                .Select(p)
                .Distinct());      
        }
    }
}