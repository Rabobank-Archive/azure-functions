using NSubstitute;
using System.Threading.Tasks;
using CompletenessCheckFunction.Activities;
using LogAnalytics.Client;
using Xunit;
using Microsoft.Azure.WebJobs;
using AutoFixture;
using LogAnalytics.Client.Response;
using Shouldly;

namespace CompletenessCheckFunction.Tests.Activities
{
    public class GetCompletedScansFromLogAnalyticsActivityTests
    {
        [Fact]
        public async Task ShouldQueryLogAnalytics()
        {
            // Arrange
            var fixture = new Fixture();
            var response = fixture.Create<LogAnalyticsQueryResponse>();

            var client = Substitute.For<ILogAnalyticsClient>();
            client.QueryAsync("").ReturnsForAnyArgs(response);

            // Act
            var fun = new GetCompletedScansFromLogAnalyticsActivity(client);
            await fun.RunAsync(Substitute.For<DurableActivityContextBase>());

            // Assert
            await client.ReceivedWithAnyArgs().QueryAsync("");
        }

        [Fact]
        public async Task ShouldRetrieveUniqueInstanceIdsFromQueryResponse()
        {
            // Arrange
            var instanceIds = new object[][]
            {
                new object[] { "1" },
                new object[] { "2" },
                new object[] { "3" },
                new object[] { "4" }
            };
            var fixture = new Fixture();
            var response = fixture.Create<LogAnalyticsQueryResponse>();
            response.tables[0].rows = instanceIds;

            var client = Substitute.For<ILogAnalyticsClient>();
            client.QueryAsync("").ReturnsForAnyArgs(response);

            // Act
            var fun = new GetCompletedScansFromLogAnalyticsActivity(client);
            var result = await fun.RunAsync(Substitute.For<DurableActivityContextBase>());

            // Assert
            result.Count.ShouldBe(4);
        }
    }
}