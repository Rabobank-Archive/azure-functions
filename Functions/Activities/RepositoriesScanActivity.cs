using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Functions.Model;
using Functions.Starters;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
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

        private async Task<ItemsExtensionData> Run(string project, string projectId, string scope)
        {
            var now = DateTime.UtcNow;
            var id = (await _azuredo.GetAsync(Requests.Project.Properties(project))).Id;
            return new ItemsExtensionData
            {
                Id = project,
                Date = DateTime.UtcNow,
                RescanUrl = ProjectScanHttpStarter.RescanUrl(_config, project, "repository"),
                HasReconcilePermissionUrl = ReconcileFunction.HasReconcilePermissionUrl(_config, id),
                Reports = await CreateReports(projectId)
            };
        }

        private async Task<IList<ItemExtensionData>> CreateReports(string projectId)
        {
            var rules = _rulesProvider.RepositoryRules(_azuredo).ToList();
            var items = _azuredo.Get(Requests.Repository.Repositories(projectId));

            var evaluationResults = new List<ItemExtensionData>();
            foreach (var repository in items)

            {
                evaluationResults.Add(new ItemExtensionData
                {
                    Item = repository.Name,
                    Rules = await rules.Evaluate(_config, projectId, "repository", repository.Id)
                });
            }

            return evaluationResults;
        }
    }
}