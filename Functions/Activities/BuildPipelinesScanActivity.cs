using Functions.Model;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;
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

            var rules = _rulesProvider.BuildRules(_azuredo).ToList();

            var fullBuildDefinition = await _azuredo.GetAsync(Builds.BuildDefinition(
                    request.Project.Id, request.BuildDefinition.Id))
                .ConfigureAwait(false);

            return new ItemExtensionData
            {
                Item = request.BuildDefinition.Name,
                ItemId = request.BuildDefinition.Id,
                Rules = await Task.WhenAll(rules.Select(async rule =>
                    new EvaluatedRule
                    {
                        Name = rule.GetType().Name,
                        Description = rule.Description,
                        Why = rule.Why,
                        IsSox = rule.IsSox,
                        Status = await rule.EvaluateAsync(request.Project.Id, fullBuildDefinition)
                            .ConfigureAwait(false),
                        Reconcile = ReconcileFunction.ReconcileFromRule(rule as IReconcile, _config,
                            request.Project.Id, RuleScopes.BuildPipelines, request.BuildDefinition.Id)
                    })
                    .ToList())
                    .ConfigureAwait(false),
                CiIdentifiers = string.Join(",", request.CiIdentifiers)
            };
        }
    }
}