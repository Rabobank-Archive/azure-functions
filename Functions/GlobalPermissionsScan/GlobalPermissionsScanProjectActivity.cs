using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;
using VstsLogAnalytics.Client;
using VstsLogAnalyticsFunction.Model;
using Project = SecurePipelineScan.VstsService.Response.Project;
using Task = System.Threading.Tasks.Task;

namespace VstsLogAnalyticsFunction.GlobalPermissionsScan
{
    public class GlobalPermissionsScanProjectActivity
    {
        private readonly ILogAnalyticsClient _analytics;
        private readonly IVstsRestClient _azuredo;
        private readonly EnvironmentConfig _config;
        private readonly IRulesProvider _rulesProvider;
        private readonly ITokenizer _tokenizer;

        public GlobalPermissionsScanProjectActivity(ILogAnalyticsClient analytics,
            IVstsRestClient azuredo,
            EnvironmentConfig config,
            IRulesProvider rulesProvider, 
            ITokenizer tokenizer)
        {
            _analytics = analytics;
            _azuredo = azuredo;
            _config = config;
            _rulesProvider = rulesProvider;
            _tokenizer = tokenizer;
        }

        [FunctionName(nameof(GlobalPermissionsScanProjectActivity))]
        public async Task RunAsActivity(
            [ActivityTrigger] DurableActivityContextBase context,
            ILogger log)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            var project = context.GetInput<Project>() ?? throw new Exception("No Project found in parameter DurableActivityContextBase");

            await Run(_config.Organization, project.Name, log);
        }

        [FunctionName("GlobalPermissionsScanProject")]
        public async Task<IActionResult> RunFromHttp(
            [HttpTrigger(AuthorizationLevel.Anonymous, Route = "scan/{organization}/{project}/globalpermissions")]
            HttpRequestMessage request,
            string organization,
            string project,
            ILogger log)
        {
            if (_tokenizer.IdentifierFromClaim(request) == null)
            {
                return new UnauthorizedResult();
            }
            
            await Run(organization, project, log);
            return new OkResult();
        }

        private async Task Run(string organization, string project, ILogger log)
        {
            log.LogInformation($"Creating preventive analysis log for project {project}");
            var now = DateTime.UtcNow;
            var rules = _rulesProvider.GlobalPermissions(_azuredo);

            var data = new GlobalPermissionsExtensionData
            {
                Id = project,
                Date = now,
                RescanUrl =  $"https://{_config.FunctionAppHostname}/api/scan/{_config.Organization}/{project}/globalpermissions",
                HasReconcilePermissionUrl = $"https://{_config.FunctionAppHostname}/api/reconcile/{_config.Organization}/{project}/haspermissions",
                Reports = rules.Select(r => new EvaluatedRule
                {
                    Name = r.GetType().Name,
                    Description = r.Description,
                    Why = r.Why,
                    Status = r.Evaluate(project),
                    Reconcile = ToReconcile(project, r as IProjectReconcile)
                }).ToList()
            };
            
            _azuredo.Put(ExtensionManagement.ExtensionData<GlobalPermissionsExtensionData>("tas", _config.ExtensionName, "globalpermissions"), data);
            foreach (var item in data.Flatten())
            {
                await _analytics.AddCustomLogJsonAsync("preventive_analysis_log", item, "evaluatedDate");
            }
            
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