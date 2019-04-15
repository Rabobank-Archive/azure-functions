using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using SecurePipelineScan.Rules;
using SecurePipelineScan.Rules.Events;
using SecurePipelineScan.Rules.Reports;
using SecurePipelineScan.VstsService;
using System;
using System.Collections.Generic;
using System.Net.Http;
using SecurePipelineScan.Rules.Security;
using VstsLogAnalytics.Client;
using VstsLogAnalytics.Common;

[assembly: WebJobsStartup(typeof(VstsLogAnalyticsFunction.Startup))]

namespace VstsLogAnalyticsFunction
{
    internal class Startup : IWebJobsStartup
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

            services.AddSingleton<IVstsRestClient>(new VstsRestClient(organization, vstsPat));

            services.AddSingleton<HttpClient>(new HttpClient());
            services.AddSingleton<IAzureServiceTokenProviderWrapper, AzureServiceTokenProviderWrapper>();

            services.AddScoped<IMemoryCache>(_ => new MemoryCache(new MemoryCacheOptions()));
            services.AddTransient<IServiceHookScan<ReleaseDeploymentCompletedReport>, ReleaseDeploymentScan>();
            services.AddTransient<IServiceHookScan<BuildScanReport>, BuildScan>();
            services.AddTransient<IProjectScan<IEnumerable<RepositoryReport>>, RepositoryScan>();
            services.AddTransient<IServiceEndpointValidator, ServiceEndpointValidator>();

            var extensionName = Environment.GetEnvironmentVariable("extensionName", EnvironmentVariableTarget.Process) ?? "tastest";
            var functionAppUrl =
                Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME", EnvironmentVariableTarget.Process) ??
                "https://azdoanalyticsdev.azurewebsites.net";
            
            var config = new EnvironmentConfig
            {
                ExtensionName = extensionName,
                Organisation = organization,
                FunctionAppHostname = functionAppUrl
            };

            services.AddSingleton<IEnvironmentConfig>(config);
            services.AddSingleton<IRuleSets, RuleSets>();
        }
    }
}