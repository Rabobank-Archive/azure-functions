using System;
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

        public RepositoriesScanActivity(EnvironmentConfig config, IVstsRestClient azuredo, 
            IRulesProvider rulesProvider)
        {
            _config = config;
            _azuredo = azuredo;
            _rulesProvider = rulesProvider;
        }

        [FunctionName(nameof(RepositoriesScanActivity))]
        public async Task<ItemExtensionData> RunAsync(
            [ActivityTrigger] RepositoriesScanActivityRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (request.Project == null)
                throw new ArgumentNullException(nameof(request.Project));
            if (request.Repository == null)
                throw new ArgumentNullException(nameof(request.Repository));
            if (request.CiIdentifiers == null)
                throw new ArgumentNullException(nameof(request.CiIdentifiers));

            var rules = _rulesProvider.RepositoryRules(_azuredo).ToList();

            return new ItemExtensionData
            {
                Item = request.Repository.Name,
                ItemId = request.Repository.Id,
                Rules = await rules.EvaluateAsync(_config, request.Project.Id, RuleScopes.Repositories, 
                    request.Repository.Id)
                        .ConfigureAwait(false),
                CiIdentifiers = String.Join(",", request.CiIdentifiers)
            };
        }
    }
}