using Functions.Model;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using Requests = SecurePipelineScan.VstsService.Requests;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Functions.Starters;
using System.Linq;

namespace Functions.Activities
{
    public class BuildPipelinesScanActivity
    {
        private readonly IRulesProvider _rulesProvider;
        private readonly EnvironmentConfig _config;
        private readonly IVstsRestClient _azuredo;

        public BuildPipelinesScanActivity(EnvironmentConfig config, IVstsRestClient azuredo, IRulesProvider rulesProvider)
        {
            _config = config;
            _azuredo = azuredo;
            _rulesProvider = rulesProvider;
        }

        [FunctionName(nameof(BuildPipelinesScanActivity))]
        public async Task<ItemsExtensionData> Run(
            [ActivityTrigger] string project)
        {
            var id = (await _azuredo.GetAsync(Requests.Project.Properties(project))).Id;
            return new ItemsExtensionData
            {
                Id = project,
                Date = DateTime.UtcNow,
                RescanUrl = ProjectScanHttpStarter.RescanUrl(_config, project, "buildpipelines"),
                HasReconcilePermissionUrl = ReconcileFunction.HasReconcilePermissionUrl(_config, id),
                Reports = await CreateReports(id)
            };
        }

        private async Task<IList<ItemExtensionData>> CreateReports(string projectId)
        {
            var rules = _rulesProvider.BuildRules(_azuredo).ToList();
            var items = _azuredo.Get(Requests.Builds.BuildDefinitions(projectId));

            var evaluationResults = new List<ItemExtensionData>();

            // Do this in a loop (instead of in a Select) to avoid parallelism which messes up our sockets
            foreach (var pipeline in items)
            {
                evaluationResults.Add(new ItemExtensionData
                {
                    Item = pipeline.Name,
                    Rules = await rules.Evaluate(_config, projectId, "buildpipelines", pipeline.Id)
                }); ;
            }
            return evaluationResults;
        }
    }
}
