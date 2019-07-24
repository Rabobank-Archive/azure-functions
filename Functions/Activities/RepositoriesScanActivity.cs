using Functions.Model;
using Functions.Starters;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Requests = SecurePipelineScan.VstsService.Requests;

namespace Functions.Activities
{
    public class RepositoriesScanActivity
    {
        private readonly IRulesProvider _rulesProvider;
        private readonly EnvironmentConfig _config;
        private readonly IVstsRestClient _azuredo;

        public RepositoriesScanActivity(EnvironmentConfig config, IVstsRestClient azuredo, IRulesProvider rulesProvider)
        {
            _config = config;
            _azuredo = azuredo;
            _rulesProvider = rulesProvider;
        }

        [FunctionName(nameof(RepositoriesScanActivity))]
        public async Task<ItemsExtensionData> Run([ActivityTrigger] Project project)
        {
            return new ItemsExtensionData
            {
                Id = project.Name,
                Date = DateTime.UtcNow,
                RescanUrl = ProjectScanHttpStarter.RescanUrl(_config, project.Name, RuleScopes.Repositories),
                HasReconcilePermissionUrl = ReconcileFunction.HasReconcilePermissionUrl(_config, project.Id),
                Reports = await CreateReports(project)
            };
        }

        private async Task<IList<ItemExtensionData>> CreateReports(Project project)
        {
            var rules = _rulesProvider.RepositoryRules(_azuredo).ToList();
            var items = _azuredo.Get(Requests.Repository.Repositories(project.Name));

            var evaluationResults = new List<ItemExtensionData>();
            foreach (var repository in items)
            {
                evaluationResults.Add(new ItemExtensionData
                {
                    Item = repository.Name,
                    Rules = await rules.Evaluate(_config, project.Id, RuleScopes.Repositories, repository.Id)
                });
            }

            return evaluationResults;
        }
    }
}