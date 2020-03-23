using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AzureDevOps.Compliance.Rules;
using AzureFunctions.TestHelpers;
using Functions.Starters;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SecurePipelineScan.VstsService;
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

            using var host = new HostBuilder()
                .ConfigureWebJobs(builder => builder
                    .AddHttp()
                    .AddDurableTask(options => options.HubName = nameof(ProjectsScan))
                    .AddAzureStorageCoreServices()
                    .ConfigureServices(services => services
                        .AddSingleton(fixture.Create<IVstsRestClient>())
                        .AddSingleton(fixture.CreateMany<IReleasePipelineRule>(3))
                        .AddSingleton(fixture.CreateMany<IProjectRule>(2))
                        .AddSingleton(fixture.CreateMany<IBuildPipelineRule>(2))
                        .AddSingleton(fixture.CreateMany<IRepositoryRule>(2))
                        .AddSingleton(CloudStorageAccount.DevelopmentStorageAccount.CreateCloudTableClient())
                        .AddSingleton(Microsoft.Azure.Storage.CloudStorageAccount.DevelopmentStorageAccount
                            .CreateCloudQueueClient())
                        .AddSingleton(fixture.Create<EnvironmentConfig>())
                        .AddSingleton(fixture.Create<IPoliciesResolver>())
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
    }
}