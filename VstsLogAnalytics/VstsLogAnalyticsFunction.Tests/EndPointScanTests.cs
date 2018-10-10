using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Moq;
using SecurePipelineScan.VstsService;
using VstsLogAnalytics.Client;
using Xunit;

namespace VstsLogAnalyticsFunction.Tests
{
    public class EndPointScanTests
    {
        [Fact]
        public void EndPointScanRun()
        {
            TimerInfo timerInfo = new TimerInfo(null, null, false);

            var logger = new Mock<ILogger>();
            var logAnalyticsClient = new Mock<ILogAnalyticsClient>();
            var vstsClient = new Mock<IVstsRestClient>();

            EndPointScan.Run(timerInfo, logAnalyticsClient.Object, vstsClient.Object, logger.Object);
        }
    }
}