using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using SecurePipelineScan.Rules;
using SecurePipelineScan.Rules.Events;
using SecurePipelineScan.Rules.Reports;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using System;
using System.Net.Http;
using LogAnalytics.Client;

[assembly: WebJobsStartup(typeof(Functions.Startup))]

namespace Functions
{
    public class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            RegisterServices(builder.Services);
        }

        private void RegisterServices(IServiceCollection services)
        {
            var tenantId = GetEnvironmentVariable("tenantId");
            var clientId = GetEnvironmentVariable("clientId");
            var clientSecret = GetEnvironmentVariable("clientSecret");

            var workspace = GetEnvironmentVariable("logAnalyticsWorkspace");
            var key = GetEnvironmentVariable("logAnalyticsKey");
            services.AddSingleton<ILogAnalyticsClient>(new LogAnalyticsClient(workspace, key,
                new AzureTokenProvider(tenantId, clientId, clientSecret)));

            var vstsPat = GetEnvironmentVariable("vstsPat");
            var organization = GetEnvironmentVariable("organization");

            services.AddSingleton<IVstsRestClient>(new VstsRestClient(organization, vstsPat));

            services.AddScoped<IMemoryCache>(_ => new MemoryCache(new MemoryCacheOptions()));
            services.AddTransient<IServiceHookScan<ReleaseDeploymentCompletedReport>, ReleaseDeploymentScan>();
            services.AddTransient<IServiceHookScan<BuildScanReport>, BuildScan>();
            services.AddTransient<IServiceEndpointValidator, ServiceEndpointValidator>();

            var extensionName = GetEnvironmentVariable("extensionName");
            var functionAppUrl = GetEnvironmentVariable("WEBSITE_HOSTNAME");

            var config = new EnvironmentConfig
            {
                ExtensionName = extensionName,
                Organization = organization,
                FunctionAppHostname = functionAppUrl,
                StorageAccountConnectionString = GetEnvironmentVariable("connectionString")
            };

            services.AddSingleton(config);
            services.AddSingleton<IRulesProvider, RulesProvider>();
            services.AddSingleton<ITokenizer>(new Tokenizer(GetEnvironmentVariable("TOKEN_SECRET")));

            services.AddSingleton(new HttpClient());
        }

        private static string GetEnvironmentVariable(string variableName)
        {
            return Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.Process)
                   ?? throw new ArgumentNullException(variableName,
                       $"Please provide a valid value for environment variable '{variableName}'");
        }
    }
}