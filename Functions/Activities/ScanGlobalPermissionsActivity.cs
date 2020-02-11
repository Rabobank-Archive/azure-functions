using Functions.Model;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.Rules.Security;
using Response = SecurePipelineScan.VstsService.Response;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System.Collections.Generic;

namespace Functions.Activities
{
    public class ScanGlobalPermissionsActivity
    {
        private readonly EnvironmentConfig _config;
        private readonly IEnumerable<IProjectRule> _rules;

        public ScanGlobalPermissionsActivity(
            EnvironmentConfig config, IEnumerable<IProjectRule> rules)
        {
            _config = config;
            _rules = rules;
        }

        [FunctionName(nameof(ScanGlobalPermissionsActivity))]
        public async Task<ItemExtensionData> RunAsync([ActivityTrigger]
            (Response.Project, string) input)
        {
            if (input.Item1 == null)
                throw new ArgumentNullException(nameof(input));

            var project = input.Item1;
            var ciIdentifiers = input.Item2;

            return new ItemExtensionData
            {
                Item = null,
                ItemId = null,
                Rules = await Task.WhenAll(_rules.Select(async r =>
                    new EvaluatedRule
                    {
                        Name = r.GetType().Name,
                        Description = r.Description,
                        Link = r.Link,
                        // TODO: fix IsSox
                        IsSox = false,
                        Status = await r.EvaluateAsync(project.Id)
                            .ConfigureAwait(false),
                        Reconcile = ReconcileFunction.ReconcileFromRule(
                            _config, project.Id, r as IProjectReconcile)
                    })
                    .ToList())
                    .ConfigureAwait(false),
                CiIdentifiers = ciIdentifiers
            };
        }
    }
}