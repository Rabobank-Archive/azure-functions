using System;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AzureFunctions.TestHelpers;
using LogAnalytics.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.WindowsAzure.Storage;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using Unmockable;
using Xunit;

namespace Functions.IntegrationTests
{
    public class TestHost : IDisposable, IAsyncLifetime
    {
        private readonly IHost _host;
        public IJobHost Jobs => _host.Services.GetService<IJobHost>();
        public TestHost()
        {
            var config = new TestConfig();
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization {ConfigureMembers = true});
            
            _host = new HostBuilder()
                .ConfigureWebJobs(builder => builder
                    .AddHttp()
                    .AddDurableTask(options => options.HubName = $"other{DateTime.Now:yyyyMMddTHHmmss}")
                    .AddAzureStorageCoreServices()
                    .ConfigureServices(services => services
                        .AddSingleton<ITokenizer>(new Tokenizer(config.ExtensionSecret))
                        .AddSingleton<IVstsRestClient>(new VstsRestClient(config.Organization, config.Token))
                        .AddSingleton<IRulesProvider>(new RulesProvider())
                        .AddSingleton(fixture.Create<ILogAnalyticsClient>())
                        .AddSingleton(fixture.Create<EnvironmentConfig>())
                        .AddSingleton(CloudStorageAccount.DevelopmentStorageAccount.CreateCloudTableClient())
                        .AddSingleton(CloudStorageAccount.DevelopmentStorageAccount.CreateCloudQueueClient().Wrap())))
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