using System;
using System.Collections.Generic;
using System.Linq;
using AzureDevOps.Compliance.Rules;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using SecurePipelineScan.VstsService.Response;
using Task = System.Threading.Tasks.Task;

namespace Functions.Orchestrators
{
    public class ProjectScanOrchestrator
    {
        private static readonly IEnumerable<string> _scopes = new[]
        {
            RuleScopes.GlobalPermissions,
            RuleScopes.ReleasePipelines,
            RuleScopes.BuildPipelines,
            RuleScopes.Repositories
        };

        [FunctionName(nameof(ProjectScanOrchestrator))]
        public async Task RunAsync([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var (project, scope, scanDate) = context.GetInput<(Project, string, DateTime)>();

            if (MustRunScope(scope, RuleScopes.GlobalPermissions))
            {
                await context.CallSubOrchestratorAsync(nameof(GlobalPermissionsOrchestrator), (project, scanDate));
            }

            if (MustRunScope(scope, RuleScopes.ReleasePipelines))
            {
                    await context.CallSubOrchestratorAsync(
                        nameof(ReleasePipelinesOrchestrator), (project, scanDate));
            }

            if (MustRunScope(scope, RuleScopes.BuildPipelines))
            {
                    await context.CallSubOrchestratorAsync(
                        nameof(BuildPipelinesOrchestrator), (project, scanDate));
            }

            if (MustRunScope(scope, RuleScopes.Repositories))
            {
                await context.CallSubOrchestratorAsync(nameof(RepositoriesOrchestrator), (project, scanDate));
            }
        }

        private static bool MustRunScope(string scope, params string[] scopes)
        {
            if (scope == null)
            {
                return true;
            }

            if (!_scopes.Contains(scope))
            {
                throw new InvalidOperationException($"invalid scope: {scope}");
            }

            return scopes.Contains(scope);
        }
    }
}