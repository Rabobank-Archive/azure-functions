using AutoFixture;
using Moq;
using RestSharp;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using VstsLogAnalytics.Client;
using Xunit;


namespace VstsLogAnalyticsFunction.Tests
{
    public class ReleaseDeploymentCompletedTests
    {
        [Fact]
        public async Task Test()
        {
            Fixture fixture = new Fixture();

            var logAnalyticsClient = new Mock<ILogAnalyticsClient>();
            var client = new Mock<IVstsRestClient>();

            client.Setup(x => x.Get(It.IsAny<IVstsRestRequest<Release>>()))
                .Returns(fixture.Create<Release>());

            client.Setup(x => x.Get(It.IsAny<IVstsRestRequest<Multiple<Repository>>>()))
                .Returns(fixture.Create<Multiple<Repository>>());

            client.Setup(x => x.Get(It.IsAny<IVstsRestRequest<Multiple<MinimumNumberOfReviewersPolicy>>>()))
                .Returns(fixture.Create<Multiple<MinimumNumberOfReviewersPolicy>>());

            var jsonEvent = ReleaseDeploymentCompletedJson();

            using (var cache = new MemoryCache(new MemoryCacheOptions()))
            {                
                await ReleaseDeploymentCompleted.Run(jsonEvent, 
                    logAnalyticsClient.Object, 
                    client.Object, 
                    cache,
                    new Mock<Microsoft.Extensions.Logging.ILogger>().Object
                    );
            }

            logAnalyticsClient.Verify(x => x.AddCustomLogJsonAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce());
        }

        private static string ReleaseDeploymentCompletedJson()
        {
            var path = Path.Combine(System.Environment.CurrentDirectory, "releasedeploymentcompleted.json");
            return File.ReadAllText(path);
        }
    }
}