using Functions.Model;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Functions.Activities
{
    public class BuildPipelinesScanActivity
    {
        private readonly IRulesProvider _rulesProvider;
        private readonly EnvironmentConfig _config;
        private readonly IVstsRestClient _azuredo;

        public BuildPipelinesScanActivity(EnvironmentConfig config, IVstsRestClient azuredo,
            IRulesProvider rulesProvider)
        {
            _config = config;
            _azuredo = azuredo;
            _rulesProvider = rulesProvider;
        }

        [FunctionName(nameof(BuildPipelinesScanActivity))]
        public async Task<ItemExtensionData> RunAsync(
            [ActivityTrigger] BuildPipelinesScanActivityRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (request.Project == null)
                throw new ArgumentNullException(nameof(request.Project));
            if (request.BuildDefinition == null)
                throw new ArgumentNullException(nameof(request.BuildDefinition));
            if (request.CiIdentifiers == null)
                throw new ArgumentNullException(nameof(request.CiIdentifiers));

            var rules = _rulesProvider.BuildRules(_azuredo).ToList();

            return new ItemExtensionData
            {
                Item = request.BuildDefinition.Name,
                ItemId = request.BuildDefinition.Id,
                Rules = await rules.EvaluateAsync(_config, request.Project.Id, RuleScopes.BuildPipelines,
                    request.BuildDefinition.Id)
                        .ConfigureAwait(false),
                CiIdentifiers = String.Join(",", request.CiIdentifiers)
            };
        }
    }
}