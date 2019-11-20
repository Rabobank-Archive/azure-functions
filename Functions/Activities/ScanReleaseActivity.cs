using System;
using System.Linq;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.VstsService.Response;

namespace Functions.Activities
{
    public class ScanReleaseActivity
    {
        [FunctionName(nameof(ScanReleaseActivity))]
        public bool Run([ActivityTrigger] Release release)
        {
            if (release == null)
                throw new ArgumentNullException(nameof(release));

            var approved = release.Environments
                .Select(e => e.PreApprovalsSnapshot)
                .Any(a => a.ApprovalOptions != null && !a.ApprovalOptions.ReleaseCreatorCanBeApprover &&
                    a.Approvals.Any(approval => !approval.IsAutomated));

            return approved;
        }
    }
}