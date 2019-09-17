using System.Collections.Generic;
using System.Threading.Tasks;
using Dynamitey.DynamicObjects;
using Functions.Activities;
using Functions.Model;
using Functions.Orchestrators;
using Microsoft.Azure.WebJobs;
using Moq;
using Xunit;

namespace Functions.Tests.Orchestrators
{
    public class ConfigurationItemsOrchestrationTests
    {
        [Fact]
        public async Task RunAsyncShouldCallGetConfigurationItemsFromTableStorageActivityOnce()
        {
            //Arrange
            var context = new Mock<DurableOrchestrationContextBase>();

            //Act
            var orchestration = new ConfigurationItemsOrchestration();
            await orchestration.RunAsync(context.Object);

            //Assert
            context.Verify(
                x => x.CallActivityAsync<List<ConfigurationItem>>(nameof(GetConfigurationItemsFromTableStorageActivity),
                    null), Times.Once);
        }
        
        [Fact]
        public async Task RunAsyncShouldCallLogAnalyticsConfigurationItemsUploadActivityOnce()
        {
            //Arrange
            var context = new Mock<DurableOrchestrationContextBase>();

            //Act
            var orchestration = new ConfigurationItemsOrchestration();
            await orchestration.RunAsync(context.Object);

            //Assert
            context.Verify(
                x => x.CallActivityAsync(nameof(LogAnalyticsConfigurationItemsUploadActivity),
                    It.IsAny<List<ConfigurationItem>>()), Times.Once);
        }
    }
}