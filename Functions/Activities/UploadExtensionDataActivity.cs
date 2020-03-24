using Functions.Model;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Functions.Activities
{
    public class UploadExtensionDataActivity
    {
        private readonly IVstsRestClient _azuredo;
        private readonly EnvironmentConfig _config;

        public UploadExtensionDataActivity(IVstsRestClient azuredo,
            EnvironmentConfig config)
        {
            _azuredo = azuredo;
            _config = config;
        }

        [FunctionName(nameof(UploadExtensionDataActivity))]
        public async Task RunAsync([ActivityTrigger] IDurableActivityContext input)
        {
            var (data, scope) = input.GetInput<(ItemsExtensionData, string)>();
            await _azuredo.PutAsync(ExtensionManagement.ExtensionData<ExtensionDataReports>(
                _config.ExtensionPublisher, _config.ExtensionName, scope), data)
                .ConfigureAwait(false);
        }
    }
}