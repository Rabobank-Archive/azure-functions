using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;
using Project = SecurePipelineScan.VstsService.Response.Project;

namespace Functions.Activities
{
    public class RepositoriesForProjectActivity
    {
        private readonly IVstsRestClient _azuredo;
        
        public RepositoriesForProjectActivity(IVstsRestClient azuredo)
        {
            _azuredo = azuredo;
        }
     
        [FunctionName(nameof(RepositoriesForProjectActivity))]
        public List<SecurePipelineScan.VstsService.Response.Repository> Run([ActivityTrigger] Project project)
        {
            var items = _azuredo.Get(Repository.Repositories(project.Id));

            return items.ToList();
        }
    }
}