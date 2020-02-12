using Functions.Helpers;
using SecurePipelineScan.Rules.Security;
using Shouldly;
using Xunit;

namespace Functions.Tests.Helpers
{
    public class SoxLoopupTests
    {
        private readonly SoxLookup soxLookUp;

        public SoxLoopupTests()
        {
            soxLookUp = new SoxLookup();
        }

        [Theory]
        [InlineData(nameof(ArtifactIsStoredSecure))]
        [InlineData(nameof(NobodyCanBypassPolicies))]
        [InlineData(nameof(NobodyCanDeleteBuilds))]
        [InlineData(nameof(NobodyCanDeleteReleases))]
        [InlineData(nameof(NobodyCanDeleteTheRepository))]
        [InlineData(nameof(NobodyCanDeleteTheTeamProject))]
        [InlineData(nameof(NobodyCanManageApprovalsAndCreateReleases))]
        [InlineData(nameof(PipelineHasAtLeastOneStageWithApproval))]
        [InlineData(nameof(PipelineHasRequiredRetentionPolicy))]
        [InlineData(nameof(ProductionStageUsesArtifactFromSecureBranch))]
        [InlineData(nameof(ReleaseBranchesProtectedByPolicies))]
        [InlineData(nameof(ReleasePipelineUsesBuildArtifact))]
        public void SoxRulesShouldBeIsSox(string soxRule)
        {
            soxLookUp.IsSox(soxRule).ShouldBe(true);
        }

        [Theory]
        [InlineData(nameof(BuildPipelineHasCredScanTask))]
        [InlineData(nameof(BuildPipelineHasFortifyTask))]
        [InlineData(nameof(BuildPipelineHasNexusIqTask))]
        [InlineData(nameof(BuildPipelineHasSonarqubeTask))]
        [InlineData(nameof(ReleasePipelineHasDeploymentMethod))]
        [InlineData(nameof(ReleasePipelineHasSm9ChangeTask))]
        [InlineData(nameof(ShouldBlockPlainTextCredentialsInPipelines))]
        public void SoxRulesShouldNotBeIsSox(string soxRule)
        {
            soxLookUp.IsSox(soxRule).ShouldBe(false);
        }
    }
}
