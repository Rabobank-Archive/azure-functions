using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Functions.Model;
using LogAnalytics.Client;
using Microsoft.Azure.WebJobs;

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
        public async Task RunAsync([ActivityTrigger](string projectName, int releaseId, 
            string releasePipelineId, IEnumerable<DeploymentMethod> deploymentMethods, bool approved) input)
        {
            if (input.projectName == null || input.releasePipelineId == null || input.deploymentMethods == null)
                throw new ArgumentOutOfRangeException(nameof(input));

            var releaseLogItem = new ReleaseLogItem
            {
                ReleaseId = input.releaseId,
                Approved = input.approved,
                CiIdentifier = ToCommaSeparatedString(input.deploymentMethods, d => d.CiIdentifier),
                CiName = ToCommaSeparatedString(input.deploymentMethods, d => d.CiName),
                ReleaseLink = new Uri($"https://dev.azure.com/{_config.Organization}/{input.projectName}" +
                    $"/_releaseProgress?_a=release-pipeline-progress&releaseId={input.releaseId}"),
                ReleasePipelineId = input.releasePipelineId,
                ReleaseStageId = ToCommaSeparatedString(input.deploymentMethods, d => d.StageId)
            };

            await _client.AddCustomLogJsonAsync("impact_analysis_log", releaseLogItem, "evaluatedDate")
                .ConfigureAwait(false);
        }

        private static string ToCommaSeparatedString(
            IEnumerable<DeploymentMethod> deploymentMethods, Func<DeploymentMethod, string> d) => 
            string.Join(",", deploymentMethods
                .Select(d)
                .Distinct());
    }
}