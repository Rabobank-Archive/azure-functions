using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Functions.Helpers;
using Functions.Model;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
using Task = System.Threading.Tasks.Task;

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
            [ActivityTrigger]
            (Project project, ReleaseDefinition releasePipeline, IList<ProductionItem> productionItems) input)
        {
            if (input.project == null || input.releasePipeline == null || input.productionItems == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            var project = input.project;
            var pipeline = input.releasePipeline;
            var productionItems = input.productionItems;
            var rules = _rulesProvider.ReleaseRules(_azuredo)
                .Where(x => !(x is ProductionStageUsesArtifactFromSecureBranch)).ToList();

            var productionStageIds = productionItems
                .SelectMany(p => p.DeploymentInfo)
                .Where(d => d.PipelineId == pipeline.Id &&
                    !string.IsNullOrWhiteSpace(d.StageId)).Select(d => d.StageId).ToList();

            return new ItemExtensionData
            {
                Item = pipeline.Name,
                ItemId = pipeline.Id,
                Rules = await Task.WhenAll(rules.Select(async rule =>
                        {
                            bool? status;
                            if (!productionStageIds.Any())
                            {
                                // if no specific stageId is provided, we assume that the rule does not
                                // need it and pass null.
                                status = await rule.EvaluateAsync(project.Id, null, pipeline)
                                    .ConfigureAwait(false);
                            }
                            else
                            {
                                // if we have stageId's, we will invoke the rule for each
                                // one of them.
                                var results = await Task.WhenAll(productionStageIds.Select(async stageId =>
                                    await rule.EvaluateAsync(project.Id, stageId, pipeline)
                                        .ConfigureAwait(false))).ConfigureAwait(false);

                                if (results.Any(r => r == null))
                                {
                                    // If any of the results above is null (could not be determined), it mean that the status
                                    // cannot be determined.
                                    status = null;
                                }
                                else
                                {
                                    // otherwise, all results must be true for the status to be true.
                                    status = results.All(r => r.HasValue && r.Value);
                                }
                            }

                            return new EvaluatedRule
                            {
                                Name = rule.GetType().Name,
                                Description = rule.Description,
                                Link = rule.Link,
                                IsSox = rule.IsSox,
                                Status = status,
                                Reconcile = ReconcileFunction.ReconcileFromRule(rule as IReconcile, _config,
                                    project.Id, RuleScopes.ReleasePipelines, pipeline.Id)
                            };
                        })
                        .ToList())
                    .ConfigureAwait(false),
                CiIdentifiers = LinkConfigurationItemHelper.GetCiIdentifiers(productionItems)
            };
        }
    }
}