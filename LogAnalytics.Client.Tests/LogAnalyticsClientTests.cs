using Flurl.Http.Testing;
using System.Threading.Tasks;
using Xunit;

namespace LogAnalytics.Client.Tests
{
    public class LogAnalyticsClientTests
    {
        [Fact]
        public void HeadersNotSetMultipleTimesWhenClientIsUsesInParallel()
        {
            var sut = new LogAnalyticsClient("adsf", "");
            using (new HttpTest())
            {
                Parallel.For(0, 100, async
                    x => await sut.AddCustomLogJsonAsync("", "", ""));
            }
        }

        [Fact]
        public async Task TestRequiredHeadersForPostToLogAnalytics()
        {
            var client = new LogAnalyticsClient("sdpfj", "");
            using (var test = new HttpTest())
            {
                await client.AddCustomLogJsonAsync("bla", "", "asdf");
                test
                    .ShouldHaveCalled("https://sdpfj.ods.opinsights.azure.com/api/logs?api-version=2016-04-01")
                    .WithHeader("Authorization")
                    .WithHeader("Log-Type", "bla")
                    .WithHeader("x-ms-date")
                    .WithHeader("time-generated-field", "asdf")
                    .WithContentType("application/json");
            }
        }
    }
}