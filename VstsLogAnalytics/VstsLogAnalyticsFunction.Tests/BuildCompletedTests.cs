using System;
using System.IO;
using System.Runtime.InteropServices;
using AutoFixture;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using SecurePipelineScan.Rules;
using SecurePipelineScan.Rules.Events;
using SecurePipelineScan.Rules.Reports;
using SecurePipelineScan.VstsService;
using VstsLogAnalytics.Client;
using Xunit;

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
                .Setup(x => x.Get(It.IsAny<IVstsRestRequest<BuildReports>>()))
                .Returns(_fixture.Create<BuildReports>());
            
            azuredo
                .Setup(x => x.Put(It.IsAny<IVstsRestRequest<BuildReports>>(), It.Is<BuildReports>(r => r.Reports.Count == 4)))
                .Verifiable();
            
            BuildCompleted.Run(
                File.ReadAllText(Path.Combine("Assets", "buildcompleted.json")),
                client.Object,
                scan.Object,
                azuredo.Object,
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
                .Setup(x => x.Get(It.IsAny<IVstsRestRequest<BuildReports>>()))
                .Returns(_fixture.Create<BuildReports>());
            
            azuredo
                .Setup(x => x.Put(It.IsAny<IVstsRestRequest<BuildReports>>(), It.Is<BuildReports>(r => r.Reports.Count == 50)))
                .Verifiable();
            
            BuildCompleted.Run(
                File.ReadAllText(Path.Combine("Assets", "buildcompleted.json")),
                new Mock<ILogAnalyticsClient>().Object,
                scan.Object,
                azuredo.Object,
                new Mock<ILogger>().Object);
            
            azuredo.Verify();
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
                .Setup(x => x.Put(It.IsAny<IVstsRestRequest<BuildReports>>(), It.IsAny<BuildReports>()))
                .Verifiable();
            
            BuildCompleted.Run(
                File.ReadAllText(Path.Combine("Assets", "buildcompleted.json")),
                new Mock<ILogAnalyticsClient>().Object,
                scan.Object,
                azuredo.Object,
                new Mock<ILogger>().Object);
            
            azuredo.Verify();
        }

        [Fact]
        public void ScanArgumentNull_ThrowsException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => BuildCompleted.Run("", new Mock<ILogAnalyticsClient>().Object, null, null, null));
            Assert.Contains("scan", ex.Message);
        }
        
        [Fact]
        public void ClientArgumentNull_ThrowsException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => BuildCompleted.Run("", null, null, null, null));
            Assert.Contains("client", ex.Message);
        }
        
        [Fact]
        public void AzureDevOpsArgumentNull_ThrowsException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => BuildCompleted.Run("", new Mock<ILogAnalyticsClient>().Object, new Mock<IServiceHookScan<BuildScanReport>>().Object, null, null));
            Assert.Contains("azuredo", ex.Message);
        }

    }
}