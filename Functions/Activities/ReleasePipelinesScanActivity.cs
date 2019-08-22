using System.Linq;
using System.Threading.Tasks;
using Functions.Model;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;

namespace Functions.Activities
{
    public class ReleasePipelinesScanActivity
    {
        private readonly IRulesProvider _rulesProvider;
        private readonly EnvironmentConfig _config;
        private readonly IVstsRestClient _azuredo;

        public ReleasePipelinesScanActivity(EnvironmentConfig config, IVstsRestClient azuredo,
            IRulesProvider rulesProvider)
        {
            _config = config;
            _azuredo = azuredo;
            _rulesProvider = rulesProvider;
        }

        [FunctionName(nameof(ReleasePipelinesScanActivity))]
        public async Task<ItemExtensionData> Run(
            [ActivityTrigger] ReleasePipelinesScanActivityRequest request)
        {
            var rules = _rulesProvider.ReleaseRules(_azuredo).ToList();

            var evaluationResult = new ItemExtensionData
            {
                Item = request.ReleaseDefinition.Name,
                Rules = await rules.Evaluate(_config, request.Project.Id, RuleScopes.ReleasePipelines,
                    request.ReleaseDefinition.Id)
            };

            return evaluationResult;
        }
    }
}