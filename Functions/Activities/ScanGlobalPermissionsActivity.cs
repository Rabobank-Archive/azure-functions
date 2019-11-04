using Functions.Model;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using Response = SecurePipelineScan.VstsService.Response;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Functions.Activities
{
    public class ScanGlobalPermissionsActivity
    {
        private readonly IVstsRestClient _azuredo;
        private readonly EnvironmentConfig _config;
        private readonly IRulesProvider _rulesProvider;

        public ScanGlobalPermissionsActivity(IVstsRestClient azuredo,
            EnvironmentConfig config, IRulesProvider rulesProvider)
        {
            _azuredo = azuredo;
            _config = config;
            _rulesProvider = rulesProvider;
        }

        [FunctionName(nameof(ScanGlobalPermissionsActivity))]
        public async Task<ItemExtensionData> RunAsync([ActivityTrigger]
            (Response.Project, IList<ProductionItem>) input)
        {
            if (input.Item1 == null)
                throw new ArgumentNullException(nameof(input));

            var project = input.Item1;
            var productionItems = input.Item2;

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
                        Link = r.Link,
                        IsSox = r.IsSox,
                        Status = await r.EvaluateAsync(project.Id)
                            .ConfigureAwait(false),
                        Reconcile = ReconcileFunction.ReconcileFromRule(
                            _config, project.Id, r as IProjectReconcile)
                    })
                    .ToList())
                    .ConfigureAwait(false),
                CiIdentifiers = string.Join(",", productionItems
                    .SelectMany(p => p.DeploymentInfo)
                    .Select(d => d.CiIdentifier)
                    .Distinct())
            };
        }
    }
}