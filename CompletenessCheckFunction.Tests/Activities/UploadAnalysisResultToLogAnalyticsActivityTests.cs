using NSubstitute;
using System.Threading.Tasks;
using AutoFixture;
using CompletenessCheckFunction.Activities;
using CompletenessCheckFunction.Requests;
using LogAnalytics.Client;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CompletenessCheckFunction.Tests.Activities
{
    public class UploadAnalysisResultToLogAnalyticsActivityTests
    {
        [Fact]
        public async Task ShouldUploadToLogAnalytics()
        {
            var fixture = new Fixture();
            
            // Arrange
            var client = Substitute.For<ILogAnalyticsClient>();
            var request = fixture.Create<UploadAnalysisResultToLogAnalyticsActivityRequest>();

            var fun = new UploadAnalysisResultToLogAnalyticsActivity(client);
            
            // Act
            await fun.RunAsync(request, Substitute.For<ILogger>());
            
            // Assert
            await client.Received().AddCustomLogJsonAsync("completeness_log", Arg.Any<CompletenessAnalysisResult[]>(),
                "AnalysisCompleted");
        }
    }
}
