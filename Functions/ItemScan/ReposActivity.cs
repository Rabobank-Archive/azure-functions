//using System;
//using System.Threading.Tasks;
//using Microsoft.Azure.WebJobs;
//using Microsoft.Extensions.Logging;
//
//namespace Functions.ItemScan
//{
//    class ReposActivity
//    {
//            
//        [FunctionName(ItemScanPermissionsActivity.ActivityNameRepos)]
//        public async Task RunAsActivityRepos(
//            [ActivityTrigger] DurableActivityContextBase context,
//            ILogger log)
//        {
//            if (context == null) throw new ArgumentNullException(nameof(context));
//            var project = context.GetInput<SecurePipelineScan.VstsService.Response.Project>() ?? throw new Exception("No Project found in parameter DurableActivityContextBase");
//
//            log.LogInformation($"Executing {ItemScanPermissionsActivity.ActivityNameRepos} for project {project.Name}");
//
//            try
//            {
//                await Run(project.Name, project.Id, "repository", log);
//                log.LogInformation($"Executed {ItemScanPermissionsActivity.ActivityNameRepos} for project {project.Name}");
//            }
//            catch (Exception)
//            {
//                log.LogInformation($"Execution failed {ItemScanPermissionsActivity.ActivityNameRepos} for project {project.Name}");
//            }
//        }
//        
//        private async Task<ItemsExtensionData> Run(string projectName, string projectId, string scope, ILogger log)
//        {
//            
//            var now = DateTime.UtcNow;
//            return new ItemsExtensionData
//            {
//                Id = projectName,
//                Date = now,
//                RescanUrl = $"https://{_config.FunctionAppHostname}/api/scan/{_config.Organization}/{projectName}/{scope}",
//                HasReconcilePermissionUrl = $"https://{_config.FunctionAppHostname}/api/reconcile/{_config.Organization}/{projectId}/haspermissions",
//                Reports = await CreateReports(projectId, scope)
//            };
//        }
//        
//        private async Task<IList<ItemExtensionData>> CreateReports(string projectId, string scope)
//        {
//            var rules = _rulesProvider.RepositoryRules(_azuredo);
//            var items = _azuredo.Get(Requests.Repository.Repositories(projectId));
//            
//            return await Task.WhenAll(items.Select(async x => new ItemExtensionData
//            {
//                Item = x.Name,
//                Rules = await Evaluate(projectId, scope, x.Id, rules)
//            }).ToList());
//        }
//    }
//}