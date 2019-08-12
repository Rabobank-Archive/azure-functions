using Functions.Model;
using SecurePipelineScan.Rules.Security;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Functions.Activities
{
    internal static class RulesExtensions
    {
        public static async Task<IList<EvaluatedRule>> Evaluate(this IEnumerable<IRule> rules,
            EnvironmentConfig environmentConfig,
            string projectId,
            string scope,
            string itemId)
        {
            return await Task.WhenAll(rules.Select(async rule => new EvaluatedRule
            {
                Name = rule.GetType().Name,
                Status = await rule.EvaluateAsync(projectId, itemId),
                Description = rule.Description,
                Why = rule.Why,
                IsSox = rule.IsSox,
                Reconcile = ReconcileFunction.ReconcileFromRule(rule as IReconcile, environmentConfig, projectId, scope, itemId)
            }).ToList());
        }
    }
}