using System.Threading.Tasks;
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
        public async Task GivenMultipleReposAllReposShouldBeSentToLogAnalytics()
        {
            Fixture fixture = new Fixture();

            var logAnalyticsClient = new Mock<ILogAnalyticsClient>();
            var vstsClient = new Mock<IVstsRestClient>(MockBehavior.Strict);

            vstsClient.Setup(client => client.Get(It.IsAny<IVstsRestRequest<Multiple<SecurePipelineScan.VstsService.Response.Project>>>()))
                .Returns(fixture.Create<Multiple<SecurePipelineScan.VstsService.Response.Project>>());

            vstsClient.Setup(client => client.Get(It.IsAny<IVstsRestRequest<Multiple<Repository>>>()))
                .Returns(fixture.Create<Multiple<Repository>>());

            vstsClient.Setup(client => client.Get(It.IsAny<IVstsRestRequest<Multiple<MinimumNumberOfReviewersPolicy>>>()))
                .Returns(fixture.Create<Multiple<MinimumNumberOfReviewersPolicy>>());

            await RepositoryScanFunction.Run(new TimerInfo(null, null, false), logAnalyticsClient.Object, vstsClient.Object, new Mock<ILogger>().Object);

            logAnalyticsClient.Verify(client => client.AddCustomLogJsonAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>()), Times.AtLeastOnce());
        }
    }
}