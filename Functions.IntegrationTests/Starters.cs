using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AzureFunctions.TestHelpers;
using Functions.Starters;
using LogAnalytics.Client;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SecurePipelineScan.Rules.Security;
using Functions.Cmdb.Client;
using SecurePipelineScan.VstsService;
using Xunit;
using Functions.Cmdb.ProductionItems;
using Functions.Helpers;

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

            using var host = new HostBuilder()
                .ConfigureWebJobs(builder => builder
                    .AddHttp()
                    .AddDurableTask(options => options.HubName = nameof(ProjectsScan))
                    .AddAzureStorageCoreServices()
                    .ConfigureServices(services => services
                        .AddSingleton(fixture.Create<IVstsRestClient>())
                        .AddDefaultRules()
                        .AddSingleton(fixture.Create<ILogAnalyticsClient>())
                        .AddSingleton(fixture.Create<ICmdbClient>())
                        .AddSingleton(CloudStorageAccount.DevelopmentStorageAccount.CreateCloudTableClient())
                        .AddSingleton(Microsoft.Azure.Storage.CloudStorageAccount.DevelopmentStorageAccount
                            .CreateCloudQueueClient())
                        .AddSingleton(fixture.Create<EnvironmentConfig>())
                                                .AddSingleton<IProductionItemsRepository, ProductionItemsRepository>()
                        .AddTransient<IProductionItemsResolver, ProductionItemsResolver>()
                        .AddSingleton<ISoxLookup, SoxLookup>()
                        .AddTransient<IReleasePipelineHasDeploymentMethodReconciler, ReleasePipelineHasDeploymentMethodReconciler>()
                        ))
                .Build();
            await host.StartAsync();

            var jobs = host.Services.GetService<IJobHost>();
            await jobs
                .Terminate()
                .Purge();

            // Act
            await jobs.CallAsync(nameof(ProjectScanStarter), new Dictionary<string, object>
            {
                ["timerInfo"] = fixture.Create<TimerInfo>()
            });

            // Assert
            await jobs
                .Ready()
                .ThrowIfFailed()
                .Purge();
        }

        [Fact]
        public async Task CompletenessScan()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization { ConfigureMembers = true });
            fixture.RepeatCount = 1;

            using var host = new HostBuilder()
                .ConfigureWebJobs(builder => builder
                    .AddHttp()
                    .AddDurableTask(options => options.HubName = nameof(CompletenessScan))
                    .AddAzureStorageCoreServices()
                    .ConfigureServices(services => services
                        .AddSingleton(fixture.Create<IVstsRestClient>())
                        .AddSingleton(fixture.Create<ILogAnalyticsClient>())
                        .AddSingleton(fixture.Create<ICmdbClient>())
                        .AddSingleton(CloudStorageAccount.DevelopmentStorageAccount.CreateCloudTableClient())
                        .AddSingleton(Microsoft.Azure.Storage.CloudStorageAccount.DevelopmentStorageAccount
                            .CreateCloudQueueClient())
                        .AddSingleton(fixture.Create<EnvironmentConfig>())
                        .AddDefaultRules()))
                .Build();
            await host.StartAsync();

            var jobs = host.Services.GetService<IJobHost>();
            await jobs
                .Terminate()
                .Purge();

            // Act
            await jobs.CallAsync(nameof(CompletenessStarter), new Dictionary<string, object>
            {
                ["timerInfo"] = fixture.Create<TimerInfo>()
            });

            // Assert
            await jobs
                .Ready()
                .ThrowIfFailed()
                .Purge();
        }
    }
}