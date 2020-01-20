using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using Functions.Activities;
using LogAnalytics.Client;
using LogAnalytics.Client.Response;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Functions.Tests.Activities
{
    public class GetScannedSupervisorsActivityTests
    {
        private readonly Fixture _fixture;

        public GetScannedSupervisorsActivityTests()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoNSubstituteCustomization());
        }

        [Fact]
        public async Task ShouldQueryLogAnalytics()
        {
            // Arrange
            var context = Substitute.For<IDurableActivityContext>();
            var response = _fixture.Create<LogAnalyticsQueryResponse>();
            var client = Substitute.For<ILogAnalyticsClient>();
            client.QueryAsync("").ReturnsForAnyArgs(response);

            // Act
            var fun = new GetScannedSupervisorsActivity(client);
            await fun.RunAsync(context);

            // Assert
            await client.ReceivedWithAnyArgs().QueryAsync("");
        }

        [Fact]
        public async Task ShouldRetrieveUniqueInstanceIdsFromQueryResponse()
        {
            // Arrange
            var instanceIds = new[]
            {
                new object[] {"1"},
                new object[] {"2"},
                new object[] {"3"},
                new object[] {"4"}
            };
            var response = _fixture.Create<LogAnalyticsQueryResponse>();
            response.tables[0].rows = instanceIds;

            var client = Substitute.For<ILogAnalyticsClient>();
            client.QueryAsync("").ReturnsForAnyArgs(response);

            // Act
            var fun = new GetScannedSupervisorsActivity(client);
            var result = await fun.RunAsync(Substitute.For<IDurableActivityContext>());

            // Assert
            result.Count.ShouldBe(4);
        }

        [Fact]
        public async Task ShouldReturnEmptyListWhenLogDoesNotExist()
        {
            // Arrange
            var context = Substitute.For<IDurableActivityContext>();
            var client = Substitute.For<ILogAnalyticsClient>();
            client.QueryAsync("").ReturnsForAnyArgs((LogAnalyticsQueryResponse)null);

            // Act
            var fun = new GetScannedSupervisorsActivity(client);
            var result = await fun.RunAsync(context);

            // Assert
            result.Count.ShouldBe(0);
        }
    }
}