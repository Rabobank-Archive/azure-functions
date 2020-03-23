using System;
using System.Text;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AzureDevOps.Compliance.Rules;
using AzureFunctions.TestHelpers;
using Flurl;
using Flurl.Http;
using Functions.Helpers;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SecurePipelineScan.VstsService;
using Xunit;
using SecurePipelineScan.VstsService.Security;

namespace Functions.IntegrationTests
{
    public class TestHost : IDisposable, IAsyncLifetime
    {
        private IHost _host;
        public IJobHost Jobs => _host.Services.GetService<IJobHost>();
        public TestConfig TestConfig { get; } = new TestConfig();

        private async Task Initialize()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization {ConfigureMembers = true});
            var environMentConfig = fixture.Create<EnvironmentConfig>();
            environMentConfig.Organization = TestConfig.Organization;

            var secret = await ExtensionSecret();

            _host = new HostBuilder()
                .ConfigureWebJobs(builder => builder
                    .AddHttp()
                    .AddDurableTask(options => options.HubName = nameof(ReconcileTest))
                    .AddAzureStorageCoreServices()
                    .ConfigureServices(services => services
                        .AddSingleton<ITokenizer>(new Tokenizer(secret))
                        .AddSingleton<IVstsRestClient>(new VstsRestClient(TestConfig.Organization, TestConfig.Token))
                        .AddDefaultRules()
                        .AddSingleton(environMentConfig)
                        .AddSingleton<IPoliciesResolver, PoliciesResolver>()
                        .AddSingleton<IMemoryCache, MemoryCache>()
                    ))
                .Build();

            await _host.StartAsync();
        }

        private Task<string> ExtensionSecret() =>
            new Url($"https://marketplace.visualstudio.com/_apis/gallery/publishers/tas/extensions/{TestConfig.ExtensionName}/certificates/latest")
                .WithBasicAuth(string.Empty, TestConfig.Token)
                .GetStringAsync();

        public void Dispose() => _host?.Dispose();

        public async Task InitializeAsync() => await Initialize();

        public Task DisposeAsync() => Task.CompletedTask;
    }
}