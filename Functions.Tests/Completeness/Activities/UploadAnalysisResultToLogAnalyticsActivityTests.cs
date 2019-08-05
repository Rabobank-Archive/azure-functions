using System.Threading.Tasks;
using AutoFixture;
using Functions.Completeness.Activities;
using Functions.Completeness.Model;
using Functions.Completeness.Requests;
using LogAnalytics.Client;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Functions.Tests.Completeness.Activities
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
