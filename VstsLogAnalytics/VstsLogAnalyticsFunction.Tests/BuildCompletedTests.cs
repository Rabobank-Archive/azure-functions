using System;
using System.IO;
using System.Runtime.InteropServices;
using AutoFixture;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using SecurePipelineScan.Rules.Events;
using SecurePipelineScan.Rules.Reports;
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
            
            BuildCompleted.Run(
                File.ReadAllText(Path.Combine("Assets", "buildcompleted.json")),
                client.Object,
                scan.Object,
                new Mock<ILogger>().Object);
            
            client.Verify();
        }

        [Fact]
        public void ScanArgumentNull_ThrowsException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => BuildCompleted.Run("", new Mock<ILogAnalyticsClient>().Object, null, null));
            Assert.Contains("scan", ex.Message);
        }
        
        [Fact]
        public void ClientArgumentNull_ThrowsException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => BuildCompleted.Run("", null, null, null));
            Assert.Contains("client", ex.Message);
        }

    }
}