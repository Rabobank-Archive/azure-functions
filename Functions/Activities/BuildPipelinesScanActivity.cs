using Functions.Model;
using Functions.Starters;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Requests = SecurePipelineScan.VstsService.Requests;

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
            [ActivityTrigger] Project project)
        {
            return new ItemsExtensionData
            {
                Id = project.Name,
                Date = DateTime.UtcNow,
                RescanUrl = ProjectScanHttpStarter.RescanUrl(_config, project.Name, "buildpipelines"),
                HasReconcilePermissionUrl = ReconcileFunction.HasReconcilePermissionUrl(_config, project.Id),
                Reports = await CreateReports(project)
            };
        }

        private async Task<IList<ItemExtensionData>> CreateReports(Project project)
        {
            var rules = _rulesProvider.BuildRules(_azuredo).ToList();
            var items = _azuredo.Get(Requests.Builds.BuildDefinitions(project.Id));

            var evaluationResults = new List<ItemExtensionData>();

            // Do this in a loop (instead of in a Select) to avoid parallelism which messes up our sockets
            foreach (var pipeline in items)
            {
                evaluationResults.Add(new ItemExtensionData
                {
                    Item = pipeline.Name,
                    Rules = await rules.Evaluate(_config, project.Id, "buildpipelines", pipeline.Id)
                }); ;
            }
            return evaluationResults;
        }
    }
}
