using SecurePipelineScan.Rules.Security;
using System.Collections.Generic;

namespace Functions.Helpers
{
    public class SoxLookup : ISoxLookup
    {
        private HashSet<string> _soxRules => new HashSet<string>
                {
                    nameof(ArtifactIsStoredSecure),
                    nameof(NobodyCanBypassPolicies),
                    nameof(NobodyCanDeleteBuilds),
                    nameof(NobodyCanDeleteReleases),
                    nameof(NobodyCanDeleteTheRepository),
                    nameof(NobodyCanDeleteTheTeamProject),
                    nameof(NobodyCanManageApprovalsAndCreateReleases),
                    nameof(PipelineHasAtLeastOneStageWithApproval),
                    nameof(PipelineHasRequiredRetentionPolicy),
                    nameof(ProductionStageUsesArtifactFromSecureBranch),
                    nameof(ReleaseBranchesProtectedByPolicies),
                    nameof(ReleasePipelineUsesBuildArtifact)
                };

        public bool IsSox(string ruleName)
        {
            if (string.IsNullOrEmpty(ruleName))
                return false;

            return _soxRules.Contains(ruleName);
        }
    }
}