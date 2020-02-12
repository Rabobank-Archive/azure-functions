using SecurePipelineScan.Rules.Security;
using System.Collections.Generic;

namespace Functions.Security
{
    public class SoxLookup
    {
        private HashSet<string> SoxRules
        {
            get
            {
                return new HashSet<string>
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
            }
        }

        public bool IsSox(string ruleName)
        {
            if (string.IsNullOrEmpty(ruleName))
                return false;

            return SoxRules.Contains(ruleName);
        }
    }
}