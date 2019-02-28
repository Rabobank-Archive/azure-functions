using AutoFixture;
using Moq;
using System.IO;
using Newtonsoft.Json.Linq;
using SecurePipelineScan.Rules.Events;
using SecurePipelineScan.Rules.Reports;
using VstsLogAnalytics.Client;
using Xunit;
using SecurePipelineScan.VstsService;
using System.Linq;
using Report = VstsLogAnalyticsFunction.ExtensionDataReports<SecurePipelineScan.Rules.Reports.ReleaseDeploymentCompletedReport>;

namespace VstsLogAnalyticsFunction.Tests
{
    public class ReleaseDeploymentCompletedScanTests
    {
        [Fact]
        public async System.Threading.Tasks.Task Test()
        {
            var fixture = new Fixture();

            var report = fixture.Create<ReleaseDeploymentCompletedReport>();            
            var logAnalyticsClient = new Mock<ILogAnalyticsClient>();
            var client = new Mock<IServiceHookScan<ReleaseDeploymentCompletedReport>>();
            client
                .Setup(x => x.Completed(It.IsAny<JObject>()))
                .Returns(report);

            fixture.Customize<Report>(r => r.With(x => x.Reports, fixture.CreateMany<ReleaseDeploymentCompletedReport>(50).ToList()));


            var azDoClient = new Mock<IVstsRestClient>();
            azDoClient.Setup(x => x.Get(It.IsAny<IVstsRestRequest<Report>>()))
                .Returns(fixture.Create<Report>());
                

            var json = ReleaseDeploymentCompletedJson();
            var fun = new ReleaseDeploymentCompletedFunction(logAnalyticsClient.Object, client.Object, azDoClient.Object);
            await fun.Run(
                json, 
                new Mock<Microsoft.Extensions.Logging.ILogger>().Object
            );

            azDoClient.Verify(x => x.Get(It.IsAny<IVstsRestRequest<Report>>()), Times.Once);
            azDoClient.Verify(x => x.Put(It.IsAny<IVstsRestRequest<Report>>(),It.Is<Report>(r => r.Reports.Count == 50)), Times.Once);

            logAnalyticsClient.Verify(x => 
                x.AddCustomLogJsonAsync(It.IsAny<string>(), report, It.IsAny<string>()), Times.AtLeastOnce());
        }

        private static string ReleaseDeploymentCompletedJson()
        {
            var path = Path.Combine("Assets", "releasedeploymentcompleted.json");
            return File.ReadAllText(path);
        }
    }
}