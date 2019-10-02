using System;
using System.Linq;
using System.Threading.Tasks;
using Functions.Model;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;

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

            var rules = _rulesProvider.ReleaseRules(_azuredo).ToList();

            var fullReleaseDefinition = await _azuredo.GetAsync(ReleaseManagement.Definition(
                    request.Project.Id, request.ReleaseDefinition.Id))
                .ConfigureAwait(false);

            return new ItemExtensionData
            {
                Item = request.ReleaseDefinition.Name,
                ItemId = request.ReleaseDefinition.Id,
                Rules = await Task.WhenAll(rules.Select(async rule =>
                    new EvaluatedRule
                    {
                        Name = rule.GetType().Name,
                        Description = rule.Description,
                        Why = rule.Why,
                        IsSox = rule.IsSox,
                        Status = await rule.EvaluateAsync(request.Project.Id, fullReleaseDefinition)
                            .ConfigureAwait(false),
                        Reconcile = ReconcileFunction.ReconcileFromRule(rule as IReconcile, _config,
                            request.Project.Id, RuleScopes.ReleasePipelines, request.ReleaseDefinition.Id)
                    })
                    .ToList())
                    .ConfigureAwait(false),
                CiIdentifiers = string.Join(",", request.CiIdentifiers)
            };
        }
    }
}