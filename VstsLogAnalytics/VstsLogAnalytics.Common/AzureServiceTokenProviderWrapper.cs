using Microsoft.Azure.Services.AppAuthentication;
using System.Threading.Tasks;

namespace VstsLogAnalytics.Common
{
    public class AzureServiceTokenProviderWrapper : IAzureServiceTokenProviderWrapper
    {
        readonly AzureServiceTokenProvider _tokenProvider = new AzureServiceTokenProvider();

        public async Task<string> GetAccessTokenAsync()
        {
            return await _tokenProvider.GetAccessTokenAsync("https://management.azure.com/");
        }
    }
}
