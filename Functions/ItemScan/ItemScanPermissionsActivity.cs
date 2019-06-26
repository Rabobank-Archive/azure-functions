//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net.Http;
//using System.Threading.Tasks;
//using Functions.Model;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Azure.WebJobs;
//using Microsoft.Azure.WebJobs.Extensions.Http;
//using Microsoft.Extensions.Logging;
//using SecurePipelineScan.Rules.Security;
//using SecurePipelineScan.VstsService;
//using Requests = SecurePipelineScan.VstsService.Requests;
//using Task = System.Threading.Tasks.Task;
//
//
//namespace Functions.ItemScan
//{
//    public abstract class ItemScanPermissionsActivity
//    {
//        private readonly IVstsRestClient _azuredo;
//        private readonly EnvironmentConfig _config;
//        private readonly ITokenizer _tokenizer;
//
//        public ItemScanPermissionsActivity(IVstsRestClient azuredo,
//            EnvironmentConfig config, 
//            ITokenizer tokenizer)
//        {
//            _azuredo = azuredo;
//            _config = config;
//            _tokenizer = tokenizer;
//        }
//
//
//        [FunctionName(nameof(ItemScanPermissionsActivity) + nameof(RunFromHttp))]
//        public async Task<IActionResult> RunFromHttp(
//            [HttpTrigger(AuthorizationLevel.Anonymous, Route = "scan/{organization}/{project}/{scope}")]
//            HttpRequestMessage request,
//            string organization,
//            string project,
//            string scope,
//            ILogger log)
//        {
//            if (_tokenizer.IdentifierFromClaim(request) == null)
//            {
//                return new UnauthorizedResult();
//            }
//
//            var properties = await _azuredo.GetAsync(Requests.Project.Properties(project));
//
//            await Run(project, properties.Id, scope);
//            return new OkResult();
//        }
//
//        
//
////        private async Task<IList<ItemExtensionData>> CreateReports(string projectId, string scope)
////        {
////            switch (scope)
////            {
////                case "repository":
////                    return await CreateReportsForRepositories(projectId, scope);
////                case "buildpipelines":
////                    return await CreateReportsForBuildPipelines(projectId, scope);
////                case "releasepipelines":
////                    return await CreateReportsForReleasePipelines(projectId, scope);
////                default:
////                    throw new ArgumentException(nameof(scope));
////            }
////        }
//
//       
//        protected async Task<ItemsExtensionData> Run(string projectName, string projectId, string scope)
//        {
//            
//            var now = DateTime.UtcNow;
//            return new ItemsExtensionData
//            {
//                Id = projectName,
//                Date = now,
//                RescanUrl = $"https://{_config.FunctionAppHostname}/api/scan/{_config.Organization}/{projectName}/{scope}",
//                HasReconcilePermissionUrl = $"https://{_config.FunctionAppHostname}/api/reconcile/{_config.Organization}/{projectId}/haspermissions",
//                Reports = await CreateReports(projectId)
//            };
//        }
//
//        protected abstract Task<IList<ItemExtensionData>> CreateReports(string projectId);
//        
//       
//    }
//}