using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureDevOps.Compliance.Rules;
using Functions.Helpers;
using Functions.Model;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Response = SecurePipelineScan.VstsService.Response;

namespace Functions.Activities
{
    public class ScanRepositoriesActivity
    {
        private readonly IEnumerable<IRepositoryRule> _rules;
        private readonly EnvironmentConfig _config;

        public ScanRepositoriesActivity(EnvironmentConfig config,
            IEnumerable<IRepositoryRule> rules)
        {
            _config = config;
            _rules = rules;
        }

        [FunctionName(nameof(ScanRepositoriesActivity))]
        public async Task<ItemExtensionData> RunAsync(
            [ActivityTrigger] (Response.Project, Response.Repository) input)
        {
            if (input.Item1 == null || input.Item2 == null)
                throw new ArgumentNullException(nameof(input));

            var project = input.Item1;
            var repository = input.Item2;

            return new ItemExtensionData
            {
                Item = repository.Name,
                ItemId = repository.Id,
                Rules = await Task.WhenAll(_rules.Select(async rule =>
                {
                    var ruleName = rule.GetType().Name;
                    return new EvaluatedRule
                    {
                        Name = ruleName,
                        Description = rule.Description,
                        Link = rule.Link,
                        Status = await rule.EvaluateAsync(project.Id, repository.Id)
                            .ConfigureAwait(false),
                        Reconcile = ReconcileFunction.ReconcileFromRule(rule as IReconcile, _config,
                            project.Id, RuleScopes.Repositories, repository.Id)
                    };
                })
                    .ToList())
                    .ConfigureAwait(false)
            };
        }
    }
}