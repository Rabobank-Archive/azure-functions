using System.Linq;
using System.Threading.Tasks;
using Functions.Model;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;

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
        public async Task<ItemExtensionData> RunAsync
            ([ActivityTrigger] Repository repository)
        {
            var rules = _rulesProvider.RepositoryRules(_azuredo).ToList();

            var evaluationResult = new ItemExtensionData
            {
                Item = repository.Name,
                Rules = await rules.EvaluateAsync(_config, repository.Project.Id, RuleScopes.Repositories, repository.Id)
                    .ConfigureAwait(false)
            };

            return evaluationResult;
        }
    }
}