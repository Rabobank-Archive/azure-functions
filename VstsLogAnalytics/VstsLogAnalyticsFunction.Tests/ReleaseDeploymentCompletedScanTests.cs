using AutoFixture;
using Moq;
using RestSharp;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
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

            var report = fixture.Create<ReleaseDeploymentCompletedReport>();            
            var logAnalyticsClient = new Mock<ILogAnalyticsClient>();
            var client = new Mock<IServiceHookScan<ReleaseDeploymentCompletedReport>>();
            client
                .Setup(x => x.Completed(It.IsAny<JObject>()))
                .Returns(report);

            var json = ReleaseDeploymentCompletedJson();
            await ReleaseDeploymentCompletedFunction.Run(
                json, 
                logAnalyticsClient.Object, 
                client.Object, 
                new Mock<Microsoft.Extensions.Logging.ILogger>().Object
            );

            logAnalyticsClient.Verify(x => 
                x.AddCustomLogJsonAsync(It.IsAny<string>(), report, It.IsAny<string>()), Times.AtLeastOnce());
        }

        private static string ReleaseDeploymentCompletedJson()
        {
            var path = Path.Combine(System.Environment.CurrentDirectory, "releasedeploymentcompleted.json");
            return File.ReadAllText(path);
        }
    }
}