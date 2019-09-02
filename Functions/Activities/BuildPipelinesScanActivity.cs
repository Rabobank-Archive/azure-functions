﻿using Functions.Model;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
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
            [ActivityTrigger] BuildDefinition definition)
        {
            var rules = _rulesProvider.BuildRules(_azuredo).ToList();

            var evaluationResult = new ItemExtensionData
            {
                Item = definition.Name,
                Rules = await rules.EvaluateAsync(_config, definition.Project.Id, RuleScopes.BuildPipelines, definition.Id)
                    .ConfigureAwait(false)
            };

            return evaluationResult;
        }
    }
}