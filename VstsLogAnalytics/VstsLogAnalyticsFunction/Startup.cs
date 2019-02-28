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
using VstsLogAnalytics.Client;
using VstsLogAnalytics.Common;

[assembly: WebJobsStartup(typeof(VstsLogAnalyticsFunction.Startup))]

namespace VstsLogAnalyticsFunction
{
    class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            RegisterServices(builder.Services);
        }

        private void RegisterServices(IServiceCollection services)
        {
            var workspace = Environment.GetEnvironmentVariable("logAnalyticsWorkspace", EnvironmentVariableTarget.Process);
            var key = Environment.GetEnvironmentVariable("logAnalyticsKey", EnvironmentVariableTarget.Process);
            services.AddSingleton<ILogAnalyticsClient>(new LogAnalyticsClient(workspace, key));

            var vstsPat = Environment.GetEnvironmentVariable("vstsPat", EnvironmentVariableTarget.Process);

            services.AddSingleton<IVstsRestClient>(new VstsRestClient("somecompany", vstsPat));
            services.AddSingleton<HttpClient>(new HttpClient());
            services.AddSingleton<IAzureServiceTokenProviderWrapper, AzureServiceTokenProviderWrapper>();

            services.AddScoped<IMemoryCache>(_ => new MemoryCache(new MemoryCacheOptions()));
            services.AddTransient<IProjectScan<SecurityReport>, SecurityReportScan>();
            services.AddTransient<IServiceHookScan<ReleaseDeploymentCompletedReport>, ReleaseDeploymentScan>();
            services.AddTransient<IServiceHookScan<BuildScanReport>, BuildScan>();
            services.AddTransient<IProjectScan<IEnumerable<RepositoryReport>>, SecurePipelineScan.Rules.RepositoryScan>();
            services.AddTransient<IServiceEndpointValidator, ServiceEndpointValidator>();
        }
    }
}