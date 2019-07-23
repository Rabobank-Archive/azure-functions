using AutoFixture;
using AutoFixture.AutoMoq;
using Flurl.Http;
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
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Report = Functions.Model.ExtensionDataReports<SecurePipelineScan.Rules.Reports.BuildScanReport>;

namespace Functions.Tests
{
    public class BuildCompletedTests
    {
        private readonly IFixture _fixture = new Fixture();

        [Fact]
        public async Task RunBuildCompletedFunction()
        {
            _fixture.Customize<Report>(r =>
                r.With(x => x.Reports, _fixture.CreateMany<BuildScanReport>(50).ToList()));
            var report = _fixture.Create<BuildScanReport>();
            var config = _fixture.Create<EnvironmentConfig>();
            var scan = new Mock<IServiceHookScan<BuildScanReport>>();
            scan
                .Setup(x => x.GetCompletedReportAsync(It.IsAny<JObject>()))
                .Returns(Task.FromResult(report));

            var logAnalyticsClient = new Mock<ILogAnalyticsClient>();

            var azDoClient = new Mock<IVstsRestClient>();
            azDoClient
                .Setup(x => x.GetAsync(It.IsAny<IVstsRequest<Report>>()))
                .Returns(Task.FromResult(_fixture.Create<Report>()));

            var json = File.ReadAllText(Path.Combine("Assets", "buildcompleted.json"));
            var function =
                new BuildCompletedFunction(logAnalyticsClient.Object, scan.Object, azDoClient.Object, config);
            await function.Run(json,
                new Mock<ILogger>().Object);

            azDoClient.Verify(x =>
                x.GetAsync(It.Is<IVstsRequest<Report>>(r => r.Resource.Contains(config.ExtensionName))), Times.Once);
            azDoClient.Verify(x =>
                x.PutAsync(It.Is<IVstsRequest<Report>>(r => r.Resource.Contains(config.ExtensionName)),
                    It.Is<Report>(r => r.Reports.Count == 50)), Times.Once);

            logAnalyticsClient.Verify(x =>
                x.AddCustomLogJsonAsync(It.IsAny<string>(), report, It.IsAny<string>()), Times.AtLeastOnce());
        }

        [Fact]
        public async Task RunBuildCompletedFunction_LimitsReports()
        {
            _fixture.RepeatCount = 50;
            var scan = new Mock<IServiceHookScan<BuildScanReport>>();
            scan
                .Setup(x => x.GetCompletedReportAsync(It.IsAny<JObject>()))
                .Returns(Task.FromResult(_fixture.Create<BuildScanReport>()));

            var azuredo = new Mock<IVstsRestClient>();
            azuredo
                .Setup(x => x.GetAsync(It.IsAny<IVstsRequest<Report>>()))
                .Returns(Task.FromResult(_fixture.Create<Report>()));

            azuredo
                .Setup(x => x.PutAsync(
                    It.IsAny<IVstsRequest<Report>>(),
                    It.Is<Report>(r => r.Reports.Count == 50)))
                .Returns(Task.FromResult(_fixture.Create<Report>()))
                .Verifiable();

            var function = new BuildCompletedFunction(new Mock<ILogAnalyticsClient>().Object, scan.Object,
                azuredo.Object, new EnvironmentConfig());
            await function.Run(File.ReadAllText(Path.Combine("Assets", "buildcompleted.json")),
                new Mock<ILogger>().Object);

            azuredo.Verify();
        }

        [Fact]
        public async Task SortedByCreatedDate()
        {
            // Arrange
            Report result = null;

            var today = new BuildScanReport { CreatedDate = DateTime.Now };
            var yesterday = new BuildScanReport { CreatedDate = DateTime.Now.Subtract(TimeSpan.FromDays(1)) };
            var tomorrow = new BuildScanReport { CreatedDate = DateTime.Now.Add(TimeSpan.FromDays(1)) };

            // Return new report from today from new scan.
            var client = new Mock<IServiceHookScan<BuildScanReport>>();
            client
                .Setup(x => x.GetCompletedReportAsync(It.IsAny<JObject>()))
                .Returns(Task.FromResult(today));

            // Return reports from yesterday and tomorrow from extension data storage
            var azdo = new Mock<IVstsRestClient>();
            azdo.Setup(x => x.GetAsync(It.IsAny<IVstsRequest<Report>>()))
                .Returns(Task.FromResult(new Report { Reports = new[] { yesterday, tomorrow }.ToList() }));

            // Capture the result to assert it later on.
            azdo.Setup(x => x.PutAsync(It.IsAny<IVstsRequest<Report>>(), It.IsAny<Report>()))
                .Returns(Task.FromResult(_fixture.Create<Report>()))
                .Callback<IVstsRequest, Report>((req, r) => result = r);

            // Act
            var fun = new BuildCompletedFunction(new Mock<ILogAnalyticsClient>().Object, client.Object, azdo.Object,
                new EnvironmentConfig());
            await fun.Run(
                File.ReadAllText(Path.Combine("Assets", "buildcompleted.json")),
                new Mock<ILogger>().Object
            );

            // Assert
            result.Reports.ShouldBe(new[] { tomorrow, today, yesterday });
        }

        [Fact]
        public async Task RunBuildCompletedFunction_FirstUpload()
        {
            var scan = new Mock<IServiceHookScan<BuildScanReport>>();
            scan
                .Setup(x => x.GetCompletedReportAsync(It.IsAny<JObject>()))
                .Returns(Task.FromResult(_fixture.Create<BuildScanReport>()));

            var azuredo = new Mock<IVstsRestClient>();
            azuredo.Setup(x => x.GetAsync(It.IsAny<IVstsRequest<Report>>()))
                .Returns(Task.FromResult((Report)null));
            azuredo
                .Setup(x => x.PutAsync(
                    It.IsAny<IVstsRequest<Report>>(),
                    It.IsAny<Report>()))
                .Returns(Task.FromResult(_fixture.Create<Report>()))
                .Verifiable();

            var function = new BuildCompletedFunction(new Mock<ILogAnalyticsClient>().Object, scan.Object,
                azuredo.Object, new EnvironmentConfig());
            await function.Run(File.ReadAllText(Path.Combine("Assets", "buildcompleted.json")),
                new Mock<ILogger>().Object);

            azuredo.Verify();
        }

        [Fact]
        public async Task RetryUpdateExtensionData()
        {
            _fixture.Customize(new AutoMoqCustomization());

            var config = _fixture.Create<EnvironmentConfig>();

            _fixture.Customize<HttpRequestMessage>(r =>
                r.With(x => x.Method, HttpMethod.Put)
                    .With(x => x.RequestUri,
                        new Uri($"https://{config.Organization}.extmgmt.visualstudio.com/blabla")));
            _fixture.Customize<HttpResponseMessage>(r => r.With(x => x.StatusCode, HttpStatusCode.BadRequest));

            //Arrange
            var scan = new Mock<IServiceHookScan<BuildScanReport>>();
            scan
                .Setup(x => x.GetCompletedReportAsync(It.IsAny<JObject>()))
                .Returns(Task.FromResult(_fixture.Create<BuildScanReport>()));

            var azuredo = new Mock<IVstsRestClient>();
            azuredo
                .Setup(x => x.GetAsync(It.IsAny<IVstsRequest<Report>>()))
                .Returns(Task.FromResult(_fixture.Create<Report>()));

            azuredo
                .SetupSequence(x => x.PutAsync(It.IsAny<IVstsRequest<Report>>(),
                    It.IsAny<Report>()))
                .Throws(new FlurlHttpException(_fixture.Create<HttpCall>(), "Some message",
                    _fixture.Create<Exception>()))
                .Returns(Task.FromResult(new Report()));

            //Act
            var function = new BuildCompletedFunction(new Mock<ILogAnalyticsClient>().Object,
                scan.Object, azuredo.Object, config);
            await function.Run(File.ReadAllText(Path.Combine("Assets", "buildcompleted.json")),
                new Mock<ILogger>().Object);

            //Assert
            azuredo.Verify(x =>
                x.PutAsync(It.IsAny<IVstsRequest<Report>>(),
                    It.IsAny<Report>()), Times.Exactly(2));
        }
    }
}