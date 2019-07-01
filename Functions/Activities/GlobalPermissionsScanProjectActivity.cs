using System;
using System.Linq;
using System.Threading.Tasks;
using Functions.Model;
using Functions.Starters;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Response;
using Task = System.Threading.Tasks.Task;

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
        public async Task<GlobalPermissionsExtensionData> RunAsActivity([ActivityTrigger] Project project)
        {
            var now = DateTime.UtcNow;
            var rules = _rulesProvider.GlobalPermissions(_azuredo);

            var data = new GlobalPermissionsExtensionData
            {
                Id = project.Name,
                Date = now,
                RescanUrl = ProjectScanHttpStarter.RescanUrl(_config, project.Name, "globalpermissions"),
                HasReconcilePermissionUrl = ReconcileFunction.HasReconcilePermissionUrl(_config, project.Name),
                Reports = await Task.WhenAll(rules.Select(async r => new EvaluatedRule
                {
                    Name = r.GetType().Name,
                    Description = r.Description,
                    Why = r.Why,
                    Status = await r.Evaluate(project.Name),
                    Reconcile = ReconcileFunction.ReconcileFromRule(_config, project.Name, r as IProjectReconcile)
                }).ToList())
            };
            
            return data;
        }
    }
}