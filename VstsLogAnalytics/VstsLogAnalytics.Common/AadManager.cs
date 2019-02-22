using Microsoft.Azure.Services.AppAuthentication;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace VstsLogAnalytics.Common
{
    public class AadManager : IAadManager
    {
        readonly AzureServiceTokenProvider tokenProvider;

        public AadManager()
        {
            tokenProvider = new AzureServiceTokenProvider();

        }

        public async Task<string> GetAccessTokenAsync()
        {
            return await tokenProvider.GetAccessTokenAsync("https://management.azure.com/");
        }
    }
}
