//using System;
//using System.Threading.Tasks;
//using Microsoft.Azure.WebJobs;
//using Microsoft.Extensions.Logging;
//
//namespace Functions.ItemScan
//{
//    class BuildsActivity
//    {
//            
//        [FunctionName(ItemScanPermissionsActivity.ActivityNameBuilds)]
//        public async Task RunAsActivityBuilds(
//            [ActivityTrigger] DurableActivityContextBase context,
//            ILogger log)
//        {
//            if (context == null) throw new ArgumentNullException(nameof(context));
//            var project = context.GetInput<SecurePipelineScan.VstsService.Response.Project>() ?? throw new Exception("No Project found in parameter DurableActivityContextBase");
//
//            log.LogInformation($"Executing {ItemScanPermissionsActivity.ActivityNameBuilds} for project {project.Name}");
//            
//            try
//            {
//                await Run(project.Name, project.Id, "buildpipelines", log);
//                log.LogInformation($"Executed {ItemScanPermissionsActivity.ActivityNameBuilds} for project {project.Name}");
//            }
//            catch (Exception)
//            {
//                log.LogInformation($"Execution failed {ItemScanPermissionsActivity.ActivityNameBuilds} for project {project.Name}");
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
//            var rules = _rulesProvider.BuildRules(_azuredo).ToList();
//            var items = _azuredo.Get(Requests.Builds.BuildDefinitions(projectId));
//
//            var evaluationResults = new List<ItemExtensionData>();
//            
//            // Do this in a loop (instead of in a Select) to avoid parallelism which messes up our sockets
//            foreach (var pipeline in items)
//            {
//                evaluationResults.Add(new ItemExtensionData
//                {
//                    Item = pipeline.Name,
//                    Rules = await Evaluate(projectId, scope, pipeline.Id, rules)
//                });
//            }
//            return evaluationResults;
//        }
//    }
//}