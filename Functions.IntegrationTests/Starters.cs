using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AzureFunctions.TestHelpers;
using Functions.Starters;
using LogAnalytics.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.WindowsAzure.Storage;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using Unmockable;
using Xunit;

namespace Functions.IntegrationTests
{
    public class Starters
    {
        [Fact]
        public async Task ProjectsScan()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization { ConfigureMembers = true });
            fixture.RepeatCount = 1;

            using (var host = new HostBuilder()
                .ConfigureWebJobs(builder => builder
                    .AddTimers()
                    .AddHttp()
                    .AddDurableTaskInTestHub()
                    .AddAzureStorageCoreServices()
                    .ConfigureServices(services => services
                        .AddSingleton(fixture.Create<IVstsRestClient>())
                        .AddSingleton(fixture.Create<ILogAnalyticsClient>())
                        .AddSingleton(CloudStorageAccount.DevelopmentStorageAccount.CreateCloudTableClient())
                        .AddSingleton(CloudStorageAccount.DevelopmentStorageAccount.CreateCloudQueueClient().Wrap())
                        .AddSingleton(fixture.Create<EnvironmentConfig>())
                        .AddSingleton(fixture.Create<IRulesProvider>())))
                    .Build())
            {
                await host.StartAsync();
                var jobs = host.Services.GetService<IJobHost>();

                // Act
                await jobs.CallAsync(nameof(ProjectScanStarter), new Dictionary<string, object>
                {
                    ["timerInfo"] = fixture.Create<TimerInfo>()
                });
                
                // Assert
                await jobs.WaitForOrchestrationsCompletion();
            }
        }
    }
}