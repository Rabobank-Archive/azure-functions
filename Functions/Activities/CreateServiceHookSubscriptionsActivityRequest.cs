using System.Collections.Generic;
using SecurePipelineScan.VstsService.Response;

namespace Functions.Activities
{
    public class CreateServiceHookSubscriptionsActivityRequest
    {
        public IList<Hook> ExistingHooks { get; set; }
        public Project Project { get; set; }
    }
}