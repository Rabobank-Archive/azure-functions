using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzDoCompliancy.CustomStatus;
using Functions.Activities;
using Functions.Helpers;
using Functions.Model;
using Functions.Starters;
using Microsoft.Azure.WebJobs;
using Response = SecurePipelineScan.VstsService.Response;

namespace Functions.Orchestrators
{
    public class ReleasePipelinesOrchestration
    {
        private readonly EnvironmentConfig _config;

        public ReleasePipelinesOrchestration(EnvironmentConfig config)
        {
            _config = config;
        }

        [FunctionName(nameof(ReleasePipelinesOrchestration))]
        public async Task<(ItemOrchestratorRequest, ItemOrchestratorRequest)> RunAsync(
            [OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            var request = context.GetInput<ItemOrchestratorRequest>();

            context.SetCustomStatus(new ScanOrchestrationStatus
            {
                Project = request.Project.Name,
                Scope = RuleScopes.ReleasePipelines
            });

            var releaseDefinitions = await context.CallActivityWithRetryAsync<List<Response.ReleaseDefinition>>(
                nameof(ReleaseDefinitionsForProjectActivity), RetryHelper.ActivityRetryOptions, request.Project);

            var data = new ItemsExtensionData
            {
                Id = request.Project.Name,
                Date = context.CurrentUtcDateTime,
                RescanUrl = ProjectScanHttpStarter.RescanUrl(
                    _config, request.Project.Name, RuleScopes.ReleasePipelines),
                HasReconcilePermissionUrl = ReconcileFunction.HasReconcilePermissionUrl(
                    _config, request.Project.Id),
                Reports = await Task.WhenAll(releaseDefinitions.Select(b =>
                    context.CallActivityWithRetryAsync<ItemExtensionData>(nameof(ReleasePipelinesScanActivity),
                        RetryHelper.ActivityRetryOptions, new ReleasePipelinesScanActivityRequest
                        {
                            Project = request.Project,
                            ReleaseDefinition = b,
                            CiIdentifiers = request.ProductionItems
                                .Where(r => r.ItemId == b.Id)
                                .SelectMany(r => r.CiIdentifiers)
                                .ToList()
                        })))
            };

            await context.CallActivityAsync(nameof(LogAnalyticsUploadActivity),
                new LogAnalyticsUploadActivityRequest
                {
                    PreventiveLogItems = data.Flatten(RuleScopes.ReleasePipelines, context.InstanceId)
                });

            await context.CallActivityAsync(nameof(ExtensionDataUploadActivity),
                (releasePipelines: data, RuleScopes.ReleasePipelines));

            var releaseBuildRepoLinks = new List<ReleaseBuildsReposLink>
            (
                await Task.WhenAll(releaseDefinitions
                .Where(r => request.ProductionItems.Select(p => p.ItemId).Contains(r.Id))
                .Select(r => context.CallActivityWithRetryAsync<ReleaseBuildsReposLink>(
                    nameof(GetReleaseBuildRepoLinksActivity), RetryHelper.ActivityRetryOptions, 
                    new ReleasePipelinesScanActivityRequest
                    {
                        Project = request.Project,
                        ReleaseDefinition = r
                    })))
            );

            return CreateItemOrchestratorRequests(releaseBuildRepoLinks, request);
        }

        public (ItemOrchestratorRequest, ItemOrchestratorRequest) CreateItemOrchestratorRequests(
            IList<ReleaseBuildsReposLink> releaseBuildRepoLinks, ItemOrchestratorRequest cisPerRelease)
        {
            var releasesPerBuild = new Dictionary<string, IList<string>>();
            releaseBuildRepoLinks
                .Where(l => l.BuildPipelineIds != null)
                .SelectMany(l => l.BuildPipelineIds)
                .Distinct()
                .ToList()
                .ForEach(b => releasesPerBuild.Add(b, releaseBuildRepoLinks
                    .Where(l => l.BuildPipelineIds != null && l.BuildPipelineIds.Contains(b))
                    .Select(l => l.ReleasePipelineId)
                    .ToList()));

            var releasesPerRepo = new Dictionary<string, IList<string>>();
            releaseBuildRepoLinks
                .Where(l => l.RepositoryIds != null)
                .SelectMany(l => l.RepositoryIds)
                .Distinct()
                .ToList()
                .ForEach(r => releasesPerRepo.Add(r, releaseBuildRepoLinks
                    .Where(l => l.RepositoryIds != null && l.RepositoryIds.Contains(r))
                    .Select(l => l.ReleasePipelineId)
                    .ToList()));

            return (
                new ItemOrchestratorRequest
                {
                    Project = cisPerRelease.Project,
                    ProductionItems = releasesPerBuild
                        .Select(b => new ProductionItem
                        {
                            ItemId = b.Key,
                            CiIdentifiers = cisPerRelease.ProductionItems
                                .Where(r => b.Value.Contains(r.ItemId))
                                .SelectMany(c => c.CiIdentifiers)
                                .Distinct()
                                .ToList()
                        })
                        .ToList()
                },
                new ItemOrchestratorRequest
                {
                    Project = cisPerRelease.Project,
                    ProductionItems = releasesPerRepo
                        .Select(b => new ProductionItem
                        {
                            ItemId = b.Key,
                            CiIdentifiers = cisPerRelease.ProductionItems
                                .Where(r => b.Value.Contains(r.ItemId))
                                .SelectMany(c => c.CiIdentifiers)
                                .Distinct()
                                .ToList()
                        })
                        .ToList()
                });
        }
    }
}