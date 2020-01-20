using System.Collections.Generic;
using System.Threading.Tasks;
using Functions.Activities;
using Functions.Model;
using Functions.Orchestrators;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Moq;
using Xunit;

namespace Functions.Tests.Orchestrators
{
    public class ConfigurationItemsOrchestratorTests
    {
        [Fact]
        public async Task RunAsyncShouldCallGetConfigurationItemsFromTableStorageActivityOnce()
        {
            //Arrange
            var context = new Mock<IDurableOrchestrationContext>();

            //Act
            var orchestration = new ConfigurationItemsOrchestrator();
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
            var context = new Mock<IDurableOrchestrationContext>();

            //Act
            var orchestration = new ConfigurationItemsOrchestrator();
            await orchestration.RunAsync(context.Object);

            //Assert
            context.Verify(
                x => x.CallActivityAsync<object>(nameof(UploadConfigurationItemLogsActivity),
                    It.IsAny<List<ConfigurationItem>>()), Times.Once);
        }
    }
}