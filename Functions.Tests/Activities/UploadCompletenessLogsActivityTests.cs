using System.Threading.Tasks;
using AutoFixture;
using Functions.Completeness.Activities;
using Functions.Completeness.Model;
using LogAnalytics.Client;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Functions.Tests.Completeness.Activities
{
    public class UploadCompletenessLogsActivityTests
    {
        [Fact]
        public async Task ShouldUploadToLogAnalytics()
        {
            var fixture = new Fixture();
            
            // Arrange
            var client = Substitute.For<ILogAnalyticsClient>();
            var request = fixture.Create<CompletenessReport>();
            var logger = Substitute.For<ILogger>();

            // Act
            var fun = new UploadCompletenessLogsActivity(client);
            await fun.RunAsync(request, logger);
            
            // Assert
            await client.Received().AddCustomLogJsonAsync("completeness_log", Arg.Any<CompletenessReport[]>(), "AnalysisCompleted");
        }
    }
}