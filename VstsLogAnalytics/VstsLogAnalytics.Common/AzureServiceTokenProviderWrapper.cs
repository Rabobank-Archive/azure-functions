﻿using Microsoft.Azure.Services.AppAuthentication;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace VstsLogAnalytics.Common
{
    public class AzureServiceTokenProviderWrapper : IAzureServiceTokenProviderWrapper
    {
        readonly AzureServiceTokenProvider tokenProvider = new AzureServiceTokenProvider();

        public async Task<string> GetAccessTokenAsync()
        {
            return await tokenProvider.GetAccessTokenAsync("https://management.azure.com/");
        }
    }
}