using AutoFixture;
using Moq;
using RestSharp;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
using System.IO;
using VstsLogAnalytics.Client;
using Xunit;

namespace VstsLogAnalyticsFunction.Tests
{
    public class ReleaseDeploymentCompletedTests
    {
        [Fact]
        public void Test()
        {
            Fixture fixture = new Fixture();

            var logAnalyticsClient = new Mock<ILogAnalyticsClient>();
            var vstsClient = new Mock<IVstsRestClient>();

            vstsClient.Setup(client => client.Execute(It.IsAny<IVstsRestRequest<SecurePipelineScan.VstsService.Response.Release>>()))
                .Returns(new RestResponse<SecurePipelineScan.VstsService.Response.Release> { Data = fixture.Create<SecurePipelineScan.VstsService.Response.Release>() });

            vstsClient.Setup(client => client.Execute(It.IsAny<IVstsRestRequest<Multiple<Repository>>>()))
                .Returns(new RestResponse<Multiple<Repository>> { Data = fixture.Create<Multiple<Repository>>() });

            vstsClient.Setup(client => client.Execute(It.IsAny<IVstsRestRequest<Multiple<MinimumNumberOfReviewersPolicy>>>()))
                .Returns(new RestResponse<Multiple<MinimumNumberOfReviewersPolicy>> { Data = fixture.Create<Multiple<MinimumNumberOfReviewersPolicy>>() });

            string jsonEvent = ReleaseDeploymentCompletedJson();

            ReleaseDeploymentCompleted.Run(jsonEvent, logAnalyticsClient.Object, vstsClient.Object, new Mock<Microsoft.Extensions.Logging.ILogger>().Object);

            logAnalyticsClient.Verify(client => client.AddCustomLogJsonAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce());
        }

        private string ReleaseDeploymentCompletedJson()
        {
            var path = Path.Combine(System.Environment.CurrentDirectory, "releasedeploymentcompleted.json");
            return File.ReadAllText(path);
        }
    }
}