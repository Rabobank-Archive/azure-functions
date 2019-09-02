using Functions.Model;
using Functions.Starters;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using System;
using System.Linq;
using System.Threading.Tasks;
using Response = SecurePipelineScan.VstsService.Response;

namespace Functions.Activities
{
    public class GlobalPermissionsScanProjectActivity
    {
        private readonly IVstsRestClient _azuredo;
        private readonly EnvironmentConfig _config;
        private readonly IRulesProvider _rulesProvider;

        public GlobalPermissionsScanProjectActivity(IVstsRestClient azuredo,
            EnvironmentConfig config,
            IRulesProvider rulesProvider)
        {
            _azuredo = azuredo;
            _config = config;
            _rulesProvider = rulesProvider;
        }

        [FunctionName(nameof(GlobalPermissionsScanProjectActivity))]
        public async Task<GlobalPermissionsExtensionData> RunAsActivityAsync([ActivityTrigger] Response.Project project)
        {
            var now = DateTime.UtcNow;
            var rules = _rulesProvider.GlobalPermissions(_azuredo);

            var data = new GlobalPermissionsExtensionData
            {
                Id = project.Name,
                Date = now,
                RescanUrl = ProjectScanHttpStarter.RescanUrl(_config, project.Name, RuleScopes.GlobalPermissions),
                HasReconcilePermissionUrl = ReconcileFunction.HasReconcilePermissionUrl(_config, project.Name),
                Reports = await Task.WhenAll(rules.Select(async r => new EvaluatedRule
                {
                    Name = r.GetType().Name,
                    Description = r.Description,
                    Why = r.Why,
                    IsSox = r.IsSox,
                    Status = await r.EvaluateAsync(project.Name).ConfigureAwait(false),
                    Reconcile = ReconcileFunction.ReconcileFromRule(_config, project.Name, r as IProjectReconcile)
                }).ToList()).ConfigureAwait(false)
            };

            return data;
        }
    }
}