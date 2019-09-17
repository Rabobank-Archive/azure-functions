using System.Collections.Generic;
using AutoFixture;
using Functions.Activities;
using Functions.Model;
using LogAnalytics.Client;
using Moq;
using Xunit;

namespace Functions.Tests.Activities
{
    public class LogAnalyticsConfigurationItemsUploadActivityTests
    {
        [Fact]
        public async void RunAsyncShouldCallAddCustomLogJsonAsyncForEveryConfigurationItem()
        {
            //Arrange
            var fixture = new Fixture();
            var mock = new Mock<ILogAnalyticsClient>();
            var configurationItems = fixture.Create<List<ConfigurationItem>>();

            var logAnalyticsConfigurationItemsUploadActivity = new LogAnalyticsConfigurationItemsUploadActivity(mock.Object);

            //Act
            await logAnalyticsConfigurationItemsUploadActivity.RunAsync(configurationItems);

            //Assert
            mock.Verify(x => x.AddCustomLogJsonAsync("configuration_item_log", It.IsAny<object>(), It.IsAny<string>()),
                Times.AtLeast(1));
        }
        
    }
}