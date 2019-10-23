using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            (Project project, ReleaseDefinition releasePipeline, IList<DeploymentMethod> deploymentMethods) input)
        {
            if (input.project == null || input.releasePipeline == null || input.deploymentMethods == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            var project = input.project;
            var pipeline = input.releasePipeline;
            var deploymentMethods = input.deploymentMethods;
            var rules = _rulesProvider.ReleaseRules(_azuredo).ToList();

            var stageIds = deploymentMethods.Where(d =>
                d.Organization == _config.Organization &&
                d.ProjectId == project.Id &&
                d.PipelineId == pipeline.Id).Select(dm => dm.StageId);

            return new ItemExtensionData
            {
                Item = pipeline.Name,
                ItemId = pipeline.Id,
                Rules = await Task.WhenAll(rules.Select(async rule =>
                        {
                            var status = (await Task.WhenAll(stageIds.Select(async stageId =>
                                    await rule.EvaluateAsync(project.Id, stageId, pipeline)
                                        .ConfigureAwait(false)))
                                .ConfigureAwait(false)).All(s => s);

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
                CiIdentifiers = string.Join(",", deploymentMethods.Select(dm => dm.CiIdentifier))
            };
        }
    }
}