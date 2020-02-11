using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Functions.Helpers;
using Functions.Model;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService.Response;
using Environment = Functions.Model.Environment;
using Task = System.Threading.Tasks.Task;

namespace Functions.Activities
{
    public class ScanReleasePipelinesActivity
    {
        private readonly IEnumerable<IReleasePipelineRule> _rules;
        private readonly EnvironmentConfig _config;

        public ScanReleasePipelinesActivity(EnvironmentConfig config,
            IEnumerable<IReleasePipelineRule> rules)
        {
            _config = config;
            _rules = rules;
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

            return new ItemExtensionData
            {
                Item = pipeline.Name,
                ItemId = pipeline.Id,
                ProjectId = project.Id,
                Environments = pipeline.Environments.Select(e => new Environment { Id = e.Id, Name = e.Name }).ToList(),
                Rules = await Task.WhenAll(_rules.Select(async rule =>
                        {
                            return new EvaluatedRule
                            {
                                Name = rule.GetType().Name,
                                Description = rule.Description,
                                Link = rule.Link,
                                // TODO: fix IsSox
                                IsSox = false,
                                Status = await rule.EvaluateAsync(project.Id, pipeline)
                                    .ConfigureAwait(false),
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