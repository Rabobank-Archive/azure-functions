using System.Collections.Generic;
using System.Threading.Tasks;
using AzureFunctions.TestHelpers;
using Dynamitey.DynamicObjects;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using Xunit;

namespace Functions.IntegrationTests
{
    public class ReconcileTest : IClassFixture<TestConfig>
    {
        private readonly TestConfig _config;

        public ReconcileTest(TestConfig config) => _config = config;

        [Theory]
        [InlineData(RuleScopes.ReleasePipelines, nameof(PipelineHasRequiredRetentionPolicy))]
        [InlineData(RuleScopes.ReleasePipelines, nameof(NobodyCanDeleteTheRepository))]
        public async Task PipelineRules(string scope, string rule)
        {
            var client = new VstsRestClient(_config.Organization, _config.Token);

            // Arrange
            using (var host = new HostBuilder()
                .ConfigureWebJobs(builder => builder
                    .AddTimers()
                    .AddHttp()
                    .AddDurableTaskInTestHub()
                    .AddAzureStorageCoreServices()
                    .ConfigureServices(services => services
                        .AddSingleton<ITokenizer>(new Tokenizer(_config.ExtensionSecret))
                        .AddSingleton<IVstsRestClient>(client)
                        .AddSingleton<IRulesProvider>(new RulesProvider())))
                .Build())
            {
                await host.StartAsync();
                var jobs = host.Services.GetService<IJobHost>();

                // Act
                var request = new DummyHttpRequest
                {
                    Scheme = "http",
                    Host = new HostString("dummy"),
                    Headers = {["Authorization"] = $"Bearer {await SessionToken(client)}"}
                };

                await jobs.CallAsync(nameof(ReconcileFunction), new Dictionary<string, object>
                {
                    ["request"] = request,
                    ["organization"] = _config.Organization,
                    ["project"] = _config.ProjectId,
                    ["scope"] = scope,
                    ["ruleName"] = rule,
                    ["item"] = _config.ReleasePipelineId
                });

                // Assert
                await jobs.WaitForOrchestrationsCompletion();
            }
        }

        private async Task<JToken> SessionToken(IVstsRestClient client)
        {
            var response = await client.PostAsync(new VstsRequest<object, JObject>(
                "_apis/WebPlatformAuth/SessionToken",
                    new Dictionary {["api-version"] = "3.2-preview.1"}),
                new
                {
                    ExtensionName = _config.ExtensionName,
                    PublisherName = "tas",
                    TokenType = 1
                });
            
            return response.SelectToken("token");
        }
    }
}