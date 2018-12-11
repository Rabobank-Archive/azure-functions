using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SecurePipelineScan.Rules.Release;
using SecurePipelineScan.VstsService;
using System;
using VstsLogAnalytics.Client;
using VstsLogAnalytics.Common;
using Requests = SecurePipelineScan.VstsService.Requests;

namespace VstsLogAnalyticsFunction
{
    public static class ReleaseDeploymentCompleted
    {
        [FunctionName("ReleaseDeploymentCompleted")]
        public static async System.Threading.Tasks.Task Run(
            [QueueTrigger("releasedeploymentcompleted", Connection = "connectionString")]string releaseCompleted,
            [Inject]ILogAnalyticsClient logAnalyticsClient,
            [Inject] IVstsRestClient client,
            ILogger log)
        {
            log.LogInformation($"Queuetriggered {nameof(ReleaseDeploymentCompleted)} by Azure Storage queue");

            var serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            var deployInfo = JsonConvert.DeserializeObject<ReleaseCompleted>(releaseCompleted, serializerSettings);

            string projectName = deployInfo.Resource.Project.Name;
            int releaseId = deployInfo.Resource.Deployment.Release.Id;
            int environmentId = deployInfo.Resource.Environment.Id;

            log.LogInformation($"release: {releaseCompleted}");

            var release = client.Execute(Requests.Release.Releases(projectName, releaseId.ToString())).Data;

            var rule = new FourEyesOnAllBuildArtefacts();
            var fourEyesResult = rule.GetResult(release, environmentId);

            var rule2 = new LastModifiedByNotTheSameAsApprovedBy();
            var LastModifiedByNotTheSameAsApprovedBy = rule2.GetResult(release);

            var deployment = new ReleaseDeployment
            {
                ReleaseId = releaseId,
                EnvironmentId = environmentId,
                ProjectName = projectName,
                FourEyesOnAllBuildArtefacts = fourEyesResult,
                LastModifiedByNotTheSameAsApprovedBy = LastModifiedByNotTheSameAsApprovedBy,
                Date = DateTime.UtcNow,
            };

            log.LogInformation("Done retrieving deployment information. Send to log analytics");

            await logAnalyticsClient.AddCustomLogJsonAsync("DeploymentStatus",
            JsonConvert.SerializeObject(deployment), "Date");
        }
    }
}