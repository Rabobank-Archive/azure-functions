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
    public class ScanRepositoriesActivity
    {
        private readonly IRulesProvider _rulesProvider;
        private readonly EnvironmentConfig _config;
        private readonly IVstsRestClient _azuredo;

        public ScanRepositoriesActivity(EnvironmentConfig config, IVstsRestClient azuredo, 
            IRulesProvider rulesProvider)
        {
            _config = config;
            _azuredo = azuredo;
            _rulesProvider = rulesProvider;
        }

        [FunctionName(nameof(ScanRepositoriesActivity))]
        public async Task<ItemExtensionData> RunAsync(
            [ActivityTrigger] (Response.Project, Response.Repository,  
            IEnumerable<Response.MinimumNumberOfReviewersPolicy>, string) input)
        {
            if (input.Item1 == null || input.Item2 == null || input.Item3 == null || input.Item4 == null)
                throw new ArgumentNullException(nameof(input));

            var project = input.Item1;
            var repository = input.Item2;
            var policies = input.Item3;
            var ciIdentifiers = input.Item4;

            var rules = _rulesProvider.RepositoryRules(_azuredo).ToList();

            return new ItemExtensionData
            {
                Item = repository.Name,
                ItemId = repository.Id,
                Rules = await Task.WhenAll(rules.Select(async rule => 
                    new EvaluatedRule
                    {
                        Name = rule.GetType().Name,
                        Description = rule.Description,
                        Link = rule.Link,
                        IsSox = rule.IsSox,
                        Status = await rule.EvaluateAsync(project.Id, repository.Id, policies)
                            .ConfigureAwait(false),
                        Reconcile = ReconcileFunction.ReconcileFromRule(rule as IReconcile, _config,
                            project.Id, RuleScopes.Repositories, repository.Id)
                    })
                    .ToList())
                    .ConfigureAwait(false),
                CiIdentifiers = ciIdentifiers
            };
        }
    }
}