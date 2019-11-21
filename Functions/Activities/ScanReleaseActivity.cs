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
                .SelectMany(e => e.PreDeployApprovals)
                .Any(a => a.ApprovedBy != null && a.ApprovedBy.Id != release.CreatedBy.Id);

            return approved;
        }
    }
}