using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;
using SecurePipelineScan.VstsService.Response;
using Project = SecurePipelineScan.VstsService.Response.Project;

namespace Functions.Activities
{
    public class ReleaseDefinitionsForProjectActivity
    {
        private readonly IVstsRestClient _azuredo;

        public ReleaseDefinitionsForProjectActivity(IVstsRestClient azuredo)
        {
            _azuredo = azuredo;
        }

        [FunctionName(nameof(ReleaseDefinitionsForProjectActivity))]
        public List<ReleaseDefinition> Run([ActivityTrigger] Project project)
        {
            var items = _azuredo.Get(ReleaseManagement.Definitions(project.Id));

            return items.ToList();
        }
    }
}