using System;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AzureFunctions.TestHelpers;
using LogAnalytics.Client;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using Xunit;

namespace Functions.IntegrationTests
{
    public class TestHost : IDisposable, IAsyncLifetime
    {
        private readonly IHost _host;
        public IJobHost Jobs => _host.Services.GetService<IJobHost>();
        public TestConfig TestConfig { get; } = new TestConfig();

        public TestHost()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization { ConfigureMembers = true });
            var environMentConfig = fixture.Create<EnvironmentConfig>();
            environMentConfig.Organization = TestConfig.Organization;

            _host = new HostBuilder()
                .ConfigureWebJobs(builder => builder
                    .AddHttp()
                    .AddDurableTask(options => options.HubName = $"other{DateTime.Now:yyyyMMddTHHmmss}")
                    .AddAzureStorageCoreServices()
                    .ConfigureServices(services => services
                        .AddSingleton<ITokenizer>(new Tokenizer(TestConfig.ExtensionSecret))
                        .AddSingleton<IVstsRestClient>(new VstsRestClient(TestConfig.Organization, TestConfig.Token))
                        .AddSingleton<IRulesProvider>(new RulesProvider())
                        .AddSingleton(fixture.Create<ILogAnalyticsClient>())
                        .AddSingleton(environMentConfig)
                        .AddSingleton(CloudStorageAccount.DevelopmentStorageAccount.CreateCloudTableClient())
                        .AddSingleton(Microsoft.Azure.Storage.CloudStorageAccount.DevelopmentStorageAccount.CreateCloudQueueClient())))
                .Build();
        }

        public void Dispose()
        {
            _host.Dispose();
        }

        public async Task InitializeAsync() => await _host.StartAsync();

        public Task DisposeAsync() => Task.CompletedTask;
    }
}