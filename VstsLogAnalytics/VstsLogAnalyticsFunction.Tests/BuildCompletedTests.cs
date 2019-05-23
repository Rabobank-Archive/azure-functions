using System;
using System.IO;
using System.Linq;
using AutoFixture;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using SecurePipelineScan.Rules.Events;
using SecurePipelineScan.Rules.Reports;
using SecurePipelineScan.VstsService;
using Shouldly;
using VstsLogAnalytics.Client;
using Xunit;
using Report = VstsLogAnalyticsFunction.ExtensionDataReports<SecurePipelineScan.Rules.Reports.BuildScanReport>;

namespace VstsLogAnalyticsFunction.Tests
{
    public class BuildCompletedTests
    {
        private readonly IFixture _fixture = new Fixture();
        
        [Fact]
        public void RunBuildCompletedFunction()
        {
            var scan = new Mock<IServiceHookScan<BuildScanReport>>();
            scan
                .Setup(x => x.Completed(It.IsAny<JObject>()))
                .Returns(_fixture.Create<BuildScanReport>());
            
            var client = new Mock<ILogAnalyticsClient>();
            client
                .Setup(x => x.AddCustomLogJsonAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>()))
                .Verifiable();

            var azuredo = new Mock<IVstsRestClient>();
            azuredo
                .Setup(x => x.Get(It.IsAny<IVstsRestRequest<Report>>()))
                .Returns(_fixture.Create<Report>());
            
            azuredo
                .Setup(x => x.Put(It.IsAny<IVstsRestRequest<Report>>(), It.Is<Report>(r => r.Reports.Count == 4)))
                .Verifiable();

            var function = new BuildCompletedFunction(client.Object, scan.Object, azuredo.Object);
            function.Run(File.ReadAllText(Path.Combine("Assets", "buildcompleted.json")),
                new Mock<ILogger>().Object);
            
            client.Verify();
            azuredo.Verify();
        }
        
        [Fact]
        public void RunBuildCompletedFunction_LimitsReports()
        {
            _fixture.RepeatCount = 50;
            
            var scan = new Mock<IServiceHookScan<BuildScanReport>>();
            scan
                .Setup(x => x.Completed(It.IsAny<JObject>()))
                .Returns(_fixture.Create<BuildScanReport>());

            var azuredo = new Mock<IVstsRestClient>();
            azuredo
                .Setup(x => x.Get(It.IsAny<IVstsRestRequest<Report>>()))
                .Returns(_fixture.Create<Report>());
            
            azuredo
                .Setup(x => x.Put(
                    It.IsAny<IVstsRestRequest<Report>>(),
                    It.Is<Report>(r => r.Reports.Count == 50)))
                .Verifiable();

            var function = new BuildCompletedFunction(new Mock<ILogAnalyticsClient>().Object, scan.Object, azuredo.Object);
            function.Run(File.ReadAllText(Path.Combine("Assets", "buildcompleted.json")),
                new Mock<ILogger>().Object);
            
            azuredo.Verify();
        }
        
        [Fact]
        public void SortedByCreatedDate()
        {
            // Arrange
            Report result = null;

            var today = new BuildScanReport { CreatedDate = DateTime.Now };
            var yesterday = new BuildScanReport { CreatedDate = DateTime.Now.Subtract(TimeSpan.FromDays(1)) };
            var tomorrow = new BuildScanReport { CreatedDate = DateTime.Now.Add(TimeSpan.FromDays(1)) };
            
            // Return new report from today from new scan.
            var client = new Mock<IServiceHookScan<BuildScanReport>>();
            client
                .Setup(x => x.Completed(It.IsAny<JObject>()))
                .Returns(today);

            // Return reports from yesterday and tomorrow from extension data storage
            var azdo = new Mock<IVstsRestClient>();
            azdo.Setup(x => x.Get(It.IsAny<IVstsRestRequest<Report>>()))
                .Returns(new Report { Reports = new[]{ yesterday, tomorrow }.ToList() });

            // Capture the result to assert it later on.
            azdo.Setup(x => x.Put(It.IsAny<IVstsRestRequest<Report>>(), It.IsAny<Report>()))
                .Callback<IVstsRestRequest, Report>((req, r) => result = r);

            // Act
            var fun = new BuildCompletedFunction(new Mock<ILogAnalyticsClient>().Object, client.Object, azdo.Object);
            fun.Run(
                File.ReadAllText(Path.Combine("Assets", "buildcompleted.json")), 
                new Mock<ILogger>().Object
            );

            // Assert
            result.Reports.ShouldBe(new[]{ tomorrow, today, yesterday });
        }

        
        [Fact]
        public void RunBuildCompletedFunction_FirstUpload()
        {
            var scan = new Mock<IServiceHookScan<BuildScanReport>>();
            scan
                .Setup(x => x.Completed(It.IsAny<JObject>()))
                .Returns(_fixture.Create<BuildScanReport>());

            var azuredo = new Mock<IVstsRestClient>();            
            azuredo
                .Setup(x => x.Put(
                    It.IsAny<IVstsRestRequest<Report>>(),
                    It.IsAny<Report>()))
                .Verifiable();

            var function = new BuildCompletedFunction(new Mock<ILogAnalyticsClient>().Object, scan.Object, azuredo.Object);
            function.Run(File.ReadAllText(Path.Combine("Assets", "buildcompleted.json")),
                new Mock<ILogger>().Object);
            
            azuredo.Verify();
        }
    }
}