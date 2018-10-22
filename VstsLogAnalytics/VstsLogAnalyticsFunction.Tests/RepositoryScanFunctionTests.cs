using AutoFixture;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Moq;
using RestSharp;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
using VstsLogAnalytics.Client;
using Xunit;

namespace VstsLogAnalyticsFunction.Tests
{
    public class RepositoryScanFunctionTests
    {
        [Fact]
        public void GivenMultipleReposAllReposShouldBeSentToLogAnalytics()
        {
            Fixture fixture = new Fixture();

            var logAnalyticsClient = new Mock<ILogAnalyticsClient>();
            var vstsClient = new Mock<IVstsRestClient>();

            vstsClient.Setup(client => client.Execute(It.IsAny<IVstsRestRequest<Multiple<Project>>>()))
                .Returns(new RestResponse<Multiple<Project>> { Data = fixture.Create<Multiple<Project>>() });

            vstsClient.Setup(client => client.Execute(It.IsAny<IVstsRestRequest<Multiple<Repository>>>()))
                .Returns(new RestResponse<Multiple<Repository>> { Data = fixture.Create<Multiple<Repository>>() });

            vstsClient.Setup(client => client.Execute(It.IsAny<IVstsRestRequest<Multiple<MinimumNumberOfReviewersPolicy>>>()))
                .Returns(new RestResponse<Multiple<MinimumNumberOfReviewersPolicy>> { Data = fixture.Create<Multiple<MinimumNumberOfReviewersPolicy>>() });

            RepositoryScanFunction.Run(new TimerInfo(null, null, false), logAnalyticsClient.Object, vstsClient.Object, new Mock<ILogger>().Object);

            logAnalyticsClient.Verify(client => client.AddCustomLogJsonAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce());
        }
    }
}