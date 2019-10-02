using Functions.Model;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Functions.Activities
{
    public class GlobalPermissionsScanActivity
    {
        private readonly IVstsRestClient _azuredo;
        private readonly EnvironmentConfig _config;
        private readonly IRulesProvider _rulesProvider;

        public GlobalPermissionsScanActivity(IVstsRestClient azuredo,
            EnvironmentConfig config, IRulesProvider rulesProvider)
        {
            _azuredo = azuredo;
            _config = config;
            _rulesProvider = rulesProvider;
        }

        [FunctionName(nameof(GlobalPermissionsScanActivity))]
        public async Task<ItemExtensionData> RunAsync([ActivityTrigger]
            ItemOrchestratorRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (request.Project == null)
                throw new ArgumentNullException(nameof(request.Project));

            var rules = _rulesProvider.GlobalPermissions(_azuredo);

            return new ItemExtensionData
            {
                Item = null,
                ItemId = null,
                Rules = await Task.WhenAll(rules.Select(async r =>
                    new EvaluatedRule
                    {
                        Name = r.GetType().Name,
                        Description = r.Description,
                        Why = r.Why,
                        IsSox = r.IsSox,
                        Status = await r.EvaluateAsync(request.Project.Id)
                            .ConfigureAwait(false),
                        Reconcile = ReconcileFunction.ReconcileFromRule(
                            _config, request.Project.Id, r as IProjectReconcile)
                    })
                    .ToList())
                    .ConfigureAwait(false),
                CiIdentifiers = string.Join(",", request.ProductionItems
                    .SelectMany(p => p.CiIdentifiers)
                    .Distinct())
            };
        }
    }
}