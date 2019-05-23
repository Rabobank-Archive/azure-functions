using System;
using AutoFixture;
using Moq;
using System.IO;
using Newtonsoft.Json.Linq;
using SecurePipelineScan.Rules.Events;
using SecurePipelineScan.Rules.Reports;
using LogAnalytics.Client;
using Xunit;
using SecurePipelineScan.VstsService;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Report = Functions.ExtensionDataReports<SecurePipelineScan.Rules.Reports.ReleaseDeploymentCompletedReport>;

namespace Functions.Tests
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

            fixture.Customize<Report>(r => r.With(x => x.Reports, fixture.CreateMany<ReleaseDeploymentCompletedReport>(50).ToList()));


            var azDoClient = new Mock<IVstsRestClient>();
            azDoClient.Setup(x => x.Get(It.IsAny<IVstsRequest<Report>>()))
                .Returns(fixture.Create<Report>());
                

            var json = ReleaseDeploymentCompletedJson();
            var fun = new ReleaseDeploymentCompletedFunction(logAnalyticsClient.Object, client.Object, azDoClient.Object);
            await fun.Run(
                json, 
                new Mock<Microsoft.Extensions.Logging.ILogger>().Object
            );

            azDoClient.Verify(x => x.Get(It.IsAny<IVstsRequest<Report>>()), Times.Once);
            azDoClient.Verify(x => x.Put(It.IsAny<IVstsRequest<Report>>(),It.Is<Report>(r => r.Reports.Count == 50)), Times.Once);

            logAnalyticsClient.Verify(x => 
                x.AddCustomLogJsonAsync(It.IsAny<string>(), report, It.IsAny<string>()), Times.AtLeastOnce());
        }

        [Fact]
        public async Task SortedByCreatedDate()
        {
            // Arrange
            Report result = null;

            var today = new ReleaseDeploymentCompletedReport { CreatedDate = DateTime.Now };
            var yesterday = new ReleaseDeploymentCompletedReport { CreatedDate = DateTime.Now.Subtract(TimeSpan.FromDays(1)) };
            var tomorrow = new ReleaseDeploymentCompletedReport { CreatedDate = DateTime.Now.Add(TimeSpan.FromDays(1)) };
            
            // Return new report from today from new scan.
            var client = new Mock<IServiceHookScan<ReleaseDeploymentCompletedReport>>();
            client
                .Setup(x => x.Completed(It.IsAny<JObject>()))
                .Returns(today);

            // Return reports from yesterday and tomorrow from extension data storage
            var azdo = new Mock<IVstsRestClient>();
            azdo.Setup(x => x.Get(It.IsAny<IVstsRequest<Report>>()))
                .Returns(new Report { Reports = new[]{ yesterday, tomorrow } });

            // Capture the result to assert it later on.
            azdo.Setup(x => x.Put(It.IsAny<IVstsRequest<Report>>(), It.IsAny<Report>()))
                .Callback<IVstsRequest, Report>((req, r) => result = r);

            // Act
            var fun = new ReleaseDeploymentCompletedFunction(new Mock<ILogAnalyticsClient>().Object, client.Object, azdo.Object);
            await fun.Run(
                ReleaseDeploymentCompletedJson(), 
                new Mock<Microsoft.Extensions.Logging.ILogger>().Object
            );

            // Assert
            result.Reports.ShouldBe(new[]{ tomorrow, today, yesterday });
        }
        
        private static string ReleaseDeploymentCompletedJson()
        {
            var path = Path.Combine("Assets", "releasedeploymentcompleted.json");
            return File.ReadAllText(path);
        }
    }
}