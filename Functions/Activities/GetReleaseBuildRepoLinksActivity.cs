using System;
using System.Linq;
using System.Threading.Tasks;
using Functions.Model;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;
using Response = SecurePipelineScan.VstsService.Response;

namespace Functions.Activities
{
    public class GetReleaseBuildRepoLinksActivity
    {
        private readonly IVstsRestClient _azuredo;

        public GetReleaseBuildRepoLinksActivity(IVstsRestClient azuredo)
        {
            _azuredo = azuredo;
        }

        [FunctionName(nameof(GetReleaseBuildRepoLinksActivity))]
        public Task<ReleaseBuildsReposLink> RunAsync(
            [ActivityTrigger] ReleasePipelinesScanActivityRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (request.Project == null)
                throw new ArgumentNullException(nameof(request.Project));
            if (request.ReleaseDefinition.Id == null)
                throw new ArgumentNullException(nameof(request.ReleaseDefinition.Id));

            return RunInternalAsync(request.Project, request.ReleaseDefinition.Id);
        }

        private async Task<ReleaseBuildsReposLink> RunInternalAsync(
            Response.Project project, string releasePipelineId)
        {
            var releasePipeline = await _azuredo.GetAsync(
                ReleaseManagement.Definition(project.Id, releasePipelineId))
                    .ConfigureAwait(false);

            return new ReleaseBuildsReposLink
            {
                ReleasePipelineId = releasePipelineId,
                BuildPipelineIds = releasePipeline.Artifacts
                    .Where(a => a.DefinitionReference != null && a.DefinitionReference.Project != null 
                        && a.DefinitionReference.Project.Id == project.Id)
                    .Where(a => a.DefinitionReference.Definition != null && a.DefinitionReference.Definition.Id != "" 
                        && a.DefinitionReference.Definition.Id != null)
                    .Select(a => a.DefinitionReference.Definition.Id)
                    .ToList(),
                RepositoryIds = releasePipeline.Artifacts
                    .Where(a => a.DefinitionReference != null && a.DefinitionReference.Project != null
                        && a.DefinitionReference.Project.Id == project.Id)
                    .Where(a => a.DefinitionReference.Repository != null && a.DefinitionReference.Repository.Id != ""
                        && a.DefinitionReference.Repository.Id != null)
                    .Select(a => a.DefinitionReference.Repository.Id)
                    .ToList()
            };
        }
    }
}