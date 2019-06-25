using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Functions.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;
using Project = SecurePipelineScan.VstsService.Response.Project;
using Task = System.Threading.Tasks.Task;

namespace Functions.GlobalPermissionsScan
{
    public class GlobalPermissionsScanProjectActivity
    {
        private readonly IVstsRestClient _azuredo;
        private readonly EnvironmentConfig _config;
        private readonly IRulesProvider _rulesProvider;
        private readonly ITokenizer _tokenizer;

        public GlobalPermissionsScanProjectActivity(IVstsRestClient azuredo,
            EnvironmentConfig config,
            IRulesProvider rulesProvider, 
            ITokenizer tokenizer)
        {
            _azuredo = azuredo;
            _config = config;
            _rulesProvider = rulesProvider;
            _tokenizer = tokenizer;
        }

        [FunctionName(nameof(GlobalPermissionsScanProjectActivity))]
        public async Task<GlobalPermissionsExtensionData> RunAsActivity(
            [ActivityTrigger] string project,
            ILogger log)
        {
            return await Run(_config.Organization, project, log);
        }

        

        private async Task<GlobalPermissionsExtensionData> Run(string organization, string project, ILogger log)
        {
            log.LogInformation($"Creating Global Permissions preventive analysis log for project {project}");
            var now = DateTime.UtcNow;
            var rules = _rulesProvider.GlobalPermissions(_azuredo);

            var data = new GlobalPermissionsExtensionData
            {
                Id = project,
                Date = now,
                RescanUrl =  $"https://{_config.FunctionAppHostname}/api/scan/{_config.Organization}/{project}/globalpermissions",
                HasReconcilePermissionUrl = $"https://{_config.FunctionAppHostname}/api/reconcile/{_config.Organization}/{project}/haspermissions",
                Reports = await Task.WhenAll(rules.Select(async r => new EvaluatedRule
                {
                    Name = r.GetType().Name,
                    Description = r.Description,
                    Why = r.Why,
                    Status = await r.Evaluate(project),
                    Reconcile = ToReconcile(project, r as IProjectReconcile)
                }).ToList())
            };
            
            return data;
        }

        private Reconcile ToReconcile(string project, IProjectReconcile rule)
        {
            return rule != null ? new Reconcile
            {
                Url = $"https://{_config.FunctionAppHostname}/api/reconcile/{_config.Organization}/{project}/globalpermissions/{rule.GetType().Name}",
                Impact = rule.Impact
            } : null;
        }
    }


}