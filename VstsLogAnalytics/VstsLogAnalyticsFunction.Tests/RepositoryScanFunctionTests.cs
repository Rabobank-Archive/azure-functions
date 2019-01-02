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
            var client = new Mock<IVstsRestClient>(MockBehavior.Strict);

            client.Setup(x => x.Get(It.IsAny<IVstsRestRequest<Multiple<Project>>>()))
                .Returns(fixture.Create<Multiple<Project>>());

            client.Setup(x => x.Get(It.IsAny<IVstsRestRequest<Multiple<Repository>>>()))
                .Returns(fixture.Create<Multiple<Repository>>());

            client.Setup(x => x.Get(It.IsAny<IVstsRestRequest<Multiple<Policy>>>()))
                .Returns(fixture.Create<Multiple<Policy>>());

            await RepositoryScanFunction.Run(new TimerInfo(null, null), logAnalyticsClient.Object, client.Object, new Mock<ILogger>().Object);

            logAnalyticsClient.Verify(x => x.AddCustomLogJsonAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>()), Times.AtLeastOnce());
        }
    }
}