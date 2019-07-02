using Functions.Model;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;
using System.Threading.Tasks;

namespace Functions.Activities
{
    public class ExtensionDataGlobalPermissionsUploadActivity
    {
        private readonly IVstsRestClient _azuredo;
        private readonly EnvironmentConfig _config;

        public ExtensionDataGlobalPermissionsUploadActivity(IVstsRestClient azuredo,
            EnvironmentConfig config)
        {
            _azuredo = azuredo;
            _config = config;
        }

        [FunctionName(nameof(ExtensionDataGlobalPermissionsUploadActivity))]
        public async Task Run([ActivityTrigger] DurableActivityContext inputs)
        {
            var (data, scope) = inputs.GetInput<(GlobalPermissionsExtensionData, string)>();
            await _azuredo.PutAsync(ExtensionManagement.ExtensionData<ExtensionDataReports>("tas", _config.ExtensionName, scope), data);
        }
    }
}
