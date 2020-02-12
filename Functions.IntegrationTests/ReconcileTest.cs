using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureFunctions.TestHelpers;
using Dynamitey.DynamicObjects;
using Functions.Model;
using Microsoft.Azure.Cosmos.Table;
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
                Headers = { ["Authorization"] = $"Bearer {token}" }
            };

            await PrepareTableStorage().ConfigureAwait(false);

            // Act
            await _host.Jobs.CallAsync(nameof(ReconcileFunction), new Dictionary<string, object>
            {
                ["request"] = request,
                ["organization"] = _config.Organization,
                ["project"] = _config.ProjectId,
                ["scope"] = scope,
                ["ruleName"] = rule,
                ["item"] = item
            }).ConfigureAwait(false);

            // Assert
            await _host.Jobs
                .Ready()
                .ThrowIfFailed()
                .Purge().ConfigureAwait(false);
        }

        private async Task PrepareTableStorage()
        {
            var tableClient = CloudStorageAccount.Parse("UseDevelopmentStorage=true").CreateCloudTableClient();
            var table = tableClient.GetTableReference("DeploymentMethod");
            await table.CreateIfNotExistsAsync().ConfigureAwait(false);

            var insertOperation = TableOperation.InsertOrReplace(new DeploymentMethod("rowKey", "partitionKey")
            {
                Organization = _config.Organization,
                ProjectId = _config.ProjectId,
                CiIdentifier = "1",
                PipelineId = _config.ReleasePipelineId,
                StageId = "2"
            });
            await table.ExecuteAsync(insertOperation).ConfigureAwait(false);
        }


        private static async Task<JToken> SessionToken(IVstsRestClient client, string publisher, string extension)
        {
            var response = await client.PostAsync(new VstsRequest<object, JObject>(
                    "_apis/WebPlatformAuth/SessionToken",
                    new Dictionary { ["api-version"] = "3.2-preview.1" }),
                new
                {
                    ExtensionName = extension,
                    PublisherName = publisher,
                    TokenType = 1
                }).ConfigureAwait(false);

            return response.SelectToken("token");
        }

        public static IEnumerable<object[]> ReleaseRules2() => Rules(
        new string[] { "NobodyCanDeleteReleases",
                               "NobodyCanManageApprovalsAndCreateReleases",
                               "PipelineHasRequiredRetentionPolicy",
                               "ReleasePipelineUsesBuildArtifact",
                               "ProductionStageUsesArtifactFromSecureBranch",
                               "PipelineHasAtLeastOneStageWithApproval",
                               "ReleasePipelineHasSm9ChangeTask",
                               "ReleasePipelineHasDeploymentMethod"}, RuleScopes.ReleasePipelines,
        new TestConfig().ReleasePipelineId);

        public static IEnumerable<object[]> ReleaseRules() => Rules(
                new string[] { "NobodyCanDeleteReleases",
                               "NobodyCanManageApprovalsAndCreateReleases",
                               "PipelineHasRequiredRetentionPolicy",
                               "ReleasePipelineUsesBuildArtifact",
                               "ProductionStageUsesArtifactFromSecureBranch",
                               "PipelineHasAtLeastOneStageWithApproval",
                               "ReleasePipelineHasSm9ChangeTask",
                               "ReleasePipelineHasDeploymentMethod"}, RuleScopes.ReleasePipelines,
                new TestConfig().ReleasePipelineId);

        public static IEnumerable<object[]> BuildRules() =>
            Rules(new string[] {
                "NobodyCanDeleteBuilds",
                "ArtifactIsStoredSecure",
                "BuildPipelineHasSonarqubeTask",
                "BuildPipelineHasFortifyTask",
                "BuildPipelineHasNexusIqTask",
                "BuildPipelineHasCredScanTask"},
                RuleScopes.BuildPipelines,
                new TestConfig().BuildPipelineId);

        public static IEnumerable<object[]> RepositoryRules() =>
            Rules(new string[] { "NobodyCanDeleteTheRepository", "ReleaseBranchesProtectedByPolicies", "NobodyCanBypassPolicies" },
                RuleScopes.Repositories,
                new TestConfig().RepositoryId);

        public static IEnumerable<object[]> GlobalPermissions() =>
            Rules(new string[] { "NobodyCanDeleteTheTeamProject", "ShouldBlockPlainTextCredentialsInPipelines" },
                RuleScopes.GlobalPermissions,
                "");

        private static IEnumerable<object[]> Rules(IEnumerable<string> rules, string scope, string item)
        {
            var skip = new string[]
            {
                // List rules you want to skip
            };

            foreach (var rule in rules.Except(skip))
            {
                yield return new object[] { scope, rule, item };
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