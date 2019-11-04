using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureFunctions.TestHelpers;
using Dynamitey.DynamicObjects;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using Xunit;

namespace Functions.IntegrationTests
{
    public class ReconcileTest : IClassFixture<TestHost>, IAsyncLifetime
    {
        private readonly TestConfig _config;
        private readonly TestHost _host;

        public ReconcileTest(TestHost host)
        {
            _config = new TestConfig();
            _host = host;
        }

        [Theory]
        [MemberData(nameof(ReleaseRules))]
        [MemberData(nameof(BuildRules))]
        [MemberData(nameof(RepositoryRules))]
        [MemberData(nameof(GlobalPermissions))]
        public async Task InvokeReconcile(string scope, string rule, string item)
        {
            // Arrange
            var client = new VstsRestClient(_config.Organization, _config.Token);
            var token = await SessionToken(client, "tas", _config.ExtensionName);
            var request = new DummyHttpRequest
            {
                Scheme = "http",
                Host = new HostString("dummy"),
                Headers = {["Authorization"] = $"Bearer {token}"}
            };

            // Act
            await _host.Jobs.CallAsync(nameof(ReconcileFunction), new Dictionary<string, object>
            {
                ["request"] = request,
                ["organization"] = _config.Organization,
                ["project"] = _config.ProjectId,
                ["scope"] = scope,
                ["ruleName"] = rule,
                ["item"] = item
            });

            // Assert
            await _host.Jobs.WaitForOrchestrationsCompletion();
        }

        private static async Task<JToken> SessionToken(IVstsRestClient client, string publisher, string extension)
        {
            var response = await client.PostAsync(new VstsRequest<object, JObject>(
                    "_apis/WebPlatformAuth/SessionToken",
                    new Dictionary {["api-version"] = "3.2-preview.1"}),
                new
                {
                    ExtensionName = extension,
                    PublisherName = publisher,
                    TokenType = 1
                });

            return response.SelectToken("token");
        }
        
        public static IEnumerable<object[]> ReleaseRules() =>
            Rules(p => p.ReleaseRules(null), 
                RuleScopes.ReleasePipelines, 
                new TestConfig().ReleasePipelineId);
        
        public static IEnumerable<object[]> BuildRules() =>
            Rules(p => p.BuildRules(null),
                RuleScopes.BuildPipelines, 
                new TestConfig().BuildPipelineId);
        
        public static IEnumerable<object[]> RepositoryRules() =>
            Rules(p => p.RepositoryRules(null),
                RuleScopes.Repositories, 
                new TestConfig().RepositoryId);

        public static IEnumerable<object[]> GlobalPermissions() =>
            Rules(p => p.GlobalPermissions(null),
                RuleScopes.GlobalPermissions, 
                "");

        private static IEnumerable<object[]> Rules(Func<IRulesProvider, IEnumerable<IRule>> rules, string scope, string item)
        {
            var provider = new RulesProvider();
            foreach (var rule in rules(provider).Select(x => x.GetType().Name))
            {
                yield return new object[] {scope, rule, item};
            }
        }

        public async Task InitializeAsync() => 
            await _host
                .Jobs
                .Terminate()
                .Purge();

        public Task DisposeAsync() => Task.CompletedTask;
    }
}