using AutoFixture;
using LogAnalytics.Client;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using SecurePipelineScan.Rules.Events;
using SecurePipelineScan.Rules.Reports;
using SecurePipelineScan.VstsService;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Report = Functions.Model.ExtensionDataReports<SecurePipelineScan.Rules.Reports.ReleaseDeploymentCompletedReport>;

namespace Functions.Tests
{
    public class ReleaseDeploymentCompletedScanTests
    {
        private readonly IFixture _fixture = new Fixture();

        [Fact]
        public async Task RunReleaseDeploymentCompletedFunction()
        {
            _fixture.Customize<Report>(r =>
                r.With(x => x.Reports, _fixture.CreateMany<ReleaseDeploymentCompletedReport>(50).ToList()));

            var report = _fixture.Create<ReleaseDeploymentCompletedReport>();
            var config = _fixture.Create<EnvironmentConfig>();
            var logAnalyticsClient = new Mock<ILogAnalyticsClient>();
            var scan = new Mock<IServiceHookScan<ReleaseDeploymentCompletedReport>>();
            scan
                .Setup(x => x.Completed(It.IsAny<JObject>()))
                .Returns(Task.FromResult(report));

            var azDoClient = new Mock<IVstsRestClient>();
            azDoClient
                .Setup(x => x.GetAsync(It.IsAny<IVstsRequest<Report>>()))
                .Returns(Task.FromResult(_fixture.Create<Report>()));

            var json = ReleaseDeploymentCompletedJson();
            var fun = new ReleaseDeploymentCompletedFunction(logAnalyticsClient.Object, scan.Object, azDoClient.Object, config);
            await fun.Run(
                json,
                new Mock<ILogger>().Object
            );

            azDoClient.Verify(x =>
                x.GetAsync(It.Is<IVstsRequest<Report>>(r => r.Resource.Contains(config.ExtensionName))), Times.Once);
            azDoClient.Verify(x =>
                x.PutAsync(It.Is<IVstsRequest<Report>>(r => r.Resource.Contains(config.ExtensionName)),
                    It.Is<Report>(r => r.Reports.Count == 50)), Times.Once);

            logAnalyticsClient.Verify(x =>
                x.AddCustomLogJsonAsync(It.IsAny<string>(), report, It.IsAny<string>()), Times.AtLeastOnce());
        }

        [Fact]
        public async void RunReleaseDeploymentCompletedFunction_FirstUpload()
        {
            var report = _fixture.Create<ReleaseDeploymentCompletedReport>();
            var logAnalyticsClient = new Mock<ILogAnalyticsClient>();
            var client = new Mock<IServiceHookScan<ReleaseDeploymentCompletedReport>>();
            client
                .Setup(x => x.Completed(It.IsAny<JObject>()))
                .Returns(Task.FromResult(report));

            var azDoClient = new Mock<IVstsRestClient>();
            azDoClient.Setup(x => x.GetAsync(It.IsAny<IVstsRequest<Report>>()))
                .Returns(Task.FromResult((Report)null));
            azDoClient
                .Setup(x => x.PutAsync(
                    It.IsAny<IVstsRequest<Report>>(),
                    It.IsAny<Report>()))
                .Returns(Task.FromResult(_fixture.Create<Report>()))
                .Verifiable();

            var config = _fixture.Create<EnvironmentConfig>();

            var json = ReleaseDeploymentCompletedJson();
            var fun = new ReleaseDeploymentCompletedFunction(logAnalyticsClient.Object, client.Object, azDoClient.Object, config);
            await fun.Run(
                json,
                new Mock<ILogger>().Object
            );
            azDoClient.Verify();
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
                .Returns(Task.FromResult(today));

            // Return reports from yesterday and tomorrow from extension data storage
            var azdo = new Mock<IVstsRestClient>();
            azdo.Setup(x => x.GetAsync(It.IsAny<IVstsRequest<Report>>()))
                .Returns(Task.FromResult(new Report { Reports = new[] { yesterday, tomorrow } }));

            // Capture the result to assert it later on.
            azdo.Setup(x => x.PutAsync(It.IsAny<IVstsRequest<Report>>(), It.IsAny<Report>()))
                .Returns(Task.FromResult(_fixture.Create<Report>()))
                .Callback<IVstsRequest, Report>((req, r) => result = r);

            // Act
            var fun = new ReleaseDeploymentCompletedFunction(new Mock<ILogAnalyticsClient>().Object, client.Object, azdo.Object, new EnvironmentConfig());
            await fun.Run(
                ReleaseDeploymentCompletedJson(),
                new Mock<ILogger>().Object
            );

            // Assert
            result.Reports.ShouldBe(new[] { tomorrow, today, yesterday });
        }

        private static string ReleaseDeploymentCompletedJson()
        {
            var path = Path.Combine("Assets", "releasedeploymentcompleted.json");
            return File.ReadAllText(path);
        }
    }
}