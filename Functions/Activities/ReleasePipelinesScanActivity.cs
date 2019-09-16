using System;
using System.Linq;
using System.Threading.Tasks;
using Functions.Model;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;

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
        public async Task<ItemExtensionData> RunAsync(
            [ActivityTrigger] ReleasePipelinesScanActivityRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (request.Project == null)
                throw new ArgumentNullException(nameof(request.Project));
            if (request.ReleaseDefinition == null)
                throw new ArgumentNullException(nameof(request.ReleaseDefinition));
            if (request.CiIdentifiers == null)
                throw new ArgumentNullException(nameof(request.CiIdentifiers));

            var rules = _rulesProvider.ReleaseRules(_azuredo).ToList();

            return new ItemExtensionData
            {
                Item = request.ReleaseDefinition.Name,
                ItemId = request.ReleaseDefinition.Id,
                Rules = await rules.EvaluateAsync(_config, request.Project.Id, RuleScopes.ReleasePipelines,
                        request.ReleaseDefinition.Id)
                    .ConfigureAwait(false),
                CiIdentifiers = String.Join(",", request.CiIdentifiers)
            };
        }
    }
}