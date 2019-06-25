using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Functions.Model;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;

namespace Functions
{
    class ExtensionDataUploadActivity
    {
        private IVstsRestClient _azuredo;
        private EnvironmentConfig _config;

        public ExtensionDataUploadActivity(IVstsRestClient azuredo,
            EnvironmentConfig config)
        {
            _azuredo = azuredo;
            _config = config;
        }

        [FunctionName(nameof(ExtensionDataUploadActivity))]
        public async Task Run([ActivityTrigger] ExtensionDataUploadActivityRequest request,
            ILogger log)
        {
            await _azuredo.PutAsync(ExtensionManagement.ExtensionData<GlobalPermissionsExtensionData>("tas", _config.ExtensionName, request.Scope), request.Data);
        }
    }
}
