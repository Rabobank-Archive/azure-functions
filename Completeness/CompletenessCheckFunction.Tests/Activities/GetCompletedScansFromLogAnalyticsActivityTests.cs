using System;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using CompletenessCheckFunction.Activities;
using LogAnalytics.Client;
using LogAnalytics.Client.Response;
using Microsoft.Azure.WebJobs;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace CompletenessCheckFunction.Tests.Activities
{
    public class GetCompletedScansFromLogAnalyticsActivityTests
    {
        private readonly Fixture _fixture;
        public GetCompletedScansFromLogAnalyticsActivityTests()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoNSubstituteCustomization());
        }

        [Fact]
        public async Task ShouldQueryLogAnalytics()
        {
            // Arrange
            var response = _fixture.Create<LogAnalyticsQueryResponse>();

            var client = Substitute.For<ILogAnalyticsClient>();
            client.QueryAsync(Arg.Any<string>()).Returns(response);

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
            client.QueryAsync(Arg.Any<string>()).Returns(response);

            // Act
            var fun = new GetCompletedScansFromLogAnalyticsActivity(client);
            var result = await fun.RunAsync(Substitute.For<DurableActivityContextBase>());

            // Assert
            result.Count.ShouldBe(4);
        }

        [Fact]
        public async Task ShouldReturnEmptyListWhenLogDoesNotExist()
        {
            // Arrange
            var client = Substitute.For<ILogAnalyticsClient>();
            client.QueryAsync(Arg.Any<string>()).Returns((LogAnalyticsQueryResponse)null);

            // Act
            var fun = new GetCompletedScansFromLogAnalyticsActivity(client);
            var result = await fun.RunAsync(Substitute.For<DurableActivityContextBase>());

            // Assert
            result.Count.ShouldBe(0);
        }
    }
}