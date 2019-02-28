using System.IO;
using AutoFixture;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using SecurePipelineScan.Rules.Events;
using SecurePipelineScan.Rules.Reports;
using SecurePipelineScan.VstsService;
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