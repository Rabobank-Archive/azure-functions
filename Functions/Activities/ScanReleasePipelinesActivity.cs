using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Functions.Model;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using Response = SecurePipelineScan.VstsService.Response;

namespace Functions.Activities
{
    public class ScanReleasePipelinesActivity
    {
        private readonly IRulesProvider _rulesProvider;
        private readonly EnvironmentConfig _config;
        private readonly IVstsRestClient _azuredo;

        public ScanReleasePipelinesActivity(EnvironmentConfig config, IVstsRestClient azuredo,
            IRulesProvider rulesProvider)
        {
            _config = config;
            _azuredo = azuredo;
            _rulesProvider = rulesProvider;
        }

        [FunctionName(nameof(ScanReleasePipelinesActivity))]
        public async Task<ItemExtensionData> RunAsync(
            [ActivityTrigger] (Response.Project, Response.ReleaseDefinition, IList<string>) input)
        {
            if (input.Item1 == null || input.Item2 == null || input.Item3 == null)
                throw new ArgumentNullException(nameof(input));

            var project = input.Item1;
            var releasePipeline = input.Item2;
            var ciIdentifiers = input.Item3;

            var rules = _rulesProvider.ReleaseRules(_azuredo).ToList();

            return new ItemExtensionData
            {
                Item = releasePipeline.Name,
                ItemId = releasePipeline.Id,
                Rules = await Task.WhenAll(rules.Select(async rule =>
                    new EvaluatedRule
                    {
                        Name = rule.GetType().Name,
                        Description = rule.Description,
                        Link = rule.Link,
                        IsSox = rule.IsSox,
                        Status = await rule.EvaluateAsync(project.Id, releasePipeline)
                            .ConfigureAwait(false),
                        Reconcile = ReconcileFunction.ReconcileFromRule(rule as IReconcile, _config,
                            project.Id, RuleScopes.ReleasePipelines, releasePipeline.Id)
                    })
                    .ToList())
                    .ConfigureAwait(false),
                CiIdentifiers = string.Join(",", ciIdentifiers)
            };
        }
    }
}