using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Moq;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
using VstsLogAnalytics.Client;
using Xunit;
using System.Collections.Generic;
using RestSharp;

namespace VstsLogAnalyticsFunction.Tests
{
    public class BranchPoliciesFunctionTests
    {
        [Fact]
        public void Method1()
        {
            TimerInfo timerInfo = new TimerInfo(null, null, false);

            var logger = new Mock<ILogger>();
            var logAnalyticsClient = new Mock<ILogAnalyticsClient>();
            var vstsClient = new Mock<IVstsRestClient>();

            var repos = new Multiple<Repository>();
            repos.Value = new List<Repository> { new Repository() { Name = "", DefaultBranch = "master", Project = new Project() { Name = "TAS" } } };

            var policies = new Multiple<MinimumNumberOfReviewersPolicy>();
            policies.Value = new List<MinimumNumberOfReviewersPolicy>();

            vstsClient.Setup(client => client.Execute(It.IsAny<IVstsRestRequest<Multiple<Repository>>>()))
                .Returns(new RestResponse<Multiple<Repository>> { Data = repos });

            vstsClient.Setup(client => client.Execute(It.IsAny<IVstsRestRequest<Multiple<MinimumNumberOfReviewersPolicy>>>()))
                .Returns(new RestResponse<Multiple<MinimumNumberOfReviewersPolicy>> { Data = policies });

            logAnalyticsClient.Setup(client => client.AddCustomLogJsonAsync(It.IsAny<string>(),It.IsAny<string>(), It.IsAny<string>()))
                .Verifiable();


            BranchPoliciesFunction.Run(timerInfo, logAnalyticsClient.Object, vstsClient.Object, logger.Object);
            logAnalyticsClient.VerifyAll();

        }
    }
}