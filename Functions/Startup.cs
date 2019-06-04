using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using SecurePipelineScan.Rules;
using SecurePipelineScan.Rules.Events;
using SecurePipelineScan.Rules.Reports;
using SecurePipelineScan.VstsService;
using System;
using System.Net.Http;
using Microsoft.Azure.Services.AppAuthentication;
using SecurePipelineScan.Rules.Security;
using Unmockable;
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
            var workspace =
                Environment.GetEnvironmentVariable("logAnalyticsWorkspace", EnvironmentVariableTarget.Process);
            var key = Environment.GetEnvironmentVariable("logAnalyticsKey", EnvironmentVariableTarget.Process);
            services.AddSingleton<ILogAnalyticsClient>(new LogAnalyticsClient(workspace, key));

            var vstsPat = Environment.GetEnvironmentVariable("vstsPat", EnvironmentVariableTarget.Process);
            var organization = Environment.GetEnvironmentVariable("organization", EnvironmentVariableTarget.Process) ?? "somecompany-test";

            services.AddSingleton<IVstsRestClient>(new VstsRestClient(organization, vstsPat, new RestClientFactory()));

            services.AddSingleton(new HttpClient());
            services.AddSingleton(new AzureServiceTokenProvider().Wrap());

            services.AddScoped<IMemoryCache>(_ => new MemoryCache(new MemoryCacheOptions()));
            services.AddTransient<IServiceHookScan<ReleaseDeploymentCompletedReport>, ReleaseDeploymentScan>();
            services.AddTransient<IServiceHookScan<BuildScanReport>, BuildScan>();
            services.AddTransient<IServiceEndpointValidator, ServiceEndpointValidator>();

            var extensionName = Environment.GetEnvironmentVariable("extensionName", EnvironmentVariableTarget.Process) ?? "tastest";
            var functionAppUrl =
                Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME", EnvironmentVariableTarget.Process) ??
                throw new ArgumentException("WEBSITE_HOSTNAME");
            
            var config = new EnvironmentConfig
            {
                ExtensionName = extensionName,
                Organization = organization,
                FunctionAppHostname = functionAppUrl,
                StorageAccountConnectionString = Environment.GetEnvironmentVariable("connectionString")
            };

            services.AddSingleton(config);
            services.AddSingleton<IRulesProvider, RulesProvider>();
            services.AddSingleton<ITokenizer>(new Tokenizer(Environment.GetEnvironmentVariable("TOKEN_SECRET")));
        }
    }
}