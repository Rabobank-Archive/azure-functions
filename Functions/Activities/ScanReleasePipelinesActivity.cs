using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureDevOps.Compliance.Rules;
using Functions.Model;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using SecurePipelineScan.VstsService.Response;
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
            (Project project, ReleaseDefinition releasePipeline) input)
        {
            if (input.project == null || input.releasePipeline == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            var project = input.project;
            var pipeline = input.releasePipeline;

            return new ItemExtensionData
            {
                Item = pipeline.Name,
                ItemId = pipeline.Id,
                ProjectId = project.Id,
                Rules = await Task.WhenAll(_rules.Select(async rule =>
                        {
                            var ruleName = rule.GetType().Name;
                            return new EvaluatedRule
                            {
                                Name = ruleName,
                                Description = rule.Description,
                                Link = rule.Link,
                                Status = await rule.EvaluateAsync(project.Id, pipeline)
                                    .ConfigureAwait(false),
                                Reconcile = ReconcileFunction.ReconcileFromRule(rule as IReconcile, _config,
                                    project.Id, RuleScopes.ReleasePipelines, pipeline.Id)
                            };
                        })
                        .ToList())
                    .ConfigureAwait(false)
            };
        }
    }
}