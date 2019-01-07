using AutoFixture;
using Moq;
using RestSharp;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using SecurePipelineScan.Rules.Events;
using SecurePipelineScan.Rules.Reports;
using VstsLogAnalytics.Client;
using Xunit;


namespace VstsLogAnalyticsFunction.Tests
{
    public class ReleaseDeploymentCompletedScanTests
    {
        [Fact]
        public async Task Test()
        {
            var fixture = new Fixture();

            var logAnalyticsClient = new Mock<ILogAnalyticsClient>();
            var client = new Mock<IVstsRestClient>();

            client.Setup(x => x.Get(It.IsAny<IVstsRestRequest<Release>>()))
                .Returns(fixture.Create<Release>());

            client.Setup(x => x.Get(It.IsAny<IVstsRestRequest<Multiple<Repository>>>()))
                .Returns(fixture.Create<Multiple<Repository>>());

            client.Setup(x => x.Get(It.IsAny<IVstsRestRequest<Multiple<MinimumNumberOfReviewersPolicy>>>()))
                .Returns(fixture.Create<Multiple<MinimumNumberOfReviewersPolicy>>());

            client.Setup(x => x.Get(It.IsAny<IVstsRestRequest<Environment>>()))
                .Returns(fixture.Create<Environment>());

            var jsonEvent = ReleaseDeploymentCompletedJson();

            using (var cache = new MemoryCache(new MemoryCacheOptions()))
            {                
                await ReleaseDeploymentCompletedFunction.Run(jsonEvent, 
                    logAnalyticsClient.Object, 
                    client.Object, 
                    cache,
                    new Mock<Microsoft.Extensions.Logging.ILogger>().Object
                );
            }

            logAnalyticsClient.Verify(x => x.AddCustomLogJsonAsync(It.IsAny<string>(), It.IsAny<ReleaseDeploymentCompletedReport>(), It.IsAny<string>()), Times.AtLeastOnce());
        }

        private static string ReleaseDeploymentCompletedJson()
        {
            var path = Path.Combine(System.Environment.CurrentDirectory, "releasedeploymentcompleted.json");
            return File.ReadAllText(path);
        }
    }
}