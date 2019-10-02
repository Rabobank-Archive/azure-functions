using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;
using SecurePipelineScan.VstsService.Response;
using Project = SecurePipelineScan.VstsService.Response.Project;

namespace Functions.Activities
{
    public class BuildDefinitionsActivity
    {
        private readonly IVstsRestClient _azuredo;

        public BuildDefinitionsActivity(IVstsRestClient azuredo)
        {
            _azuredo = azuredo;
        }

        [FunctionName(nameof(BuildDefinitionsActivity))]
        public IList<BuildDefinition> Run([ActivityTrigger] Project project)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project));

            var items = _azuredo.Get(Builds.BuildDefinitions(project.Id));

            return items.ToList();
        }
    }
}