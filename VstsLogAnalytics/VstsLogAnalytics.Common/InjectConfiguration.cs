using Microsoft.Azure.WebJobs.Host.Config;
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

[assembly: WebJobsStartup(typeof(WebJobsExtensionStartup), "VstsLogAnalyticsFunction")]

namespace VstsLogAnalytics.Common
{
    public class InjectConfiguration : IExtensionConfigProvider
    {
        public void Initialize(ExtensionConfigContext context)
        {
            var services = new ServiceCollection();
            RegisterServices(services);
            var serviceProvider = services.BuildServiceProvider(true);

            context
                .AddBindingRule<InjectAttribute>()
                .Bind(new InjectBindingProvider(serviceProvider));
        }

        private void RegisterServices(IServiceCollection services)
        {
            var workspace = Environment.GetEnvironmentVariable("logAnalyticsWorkspace", EnvironmentVariableTarget.Process);
            var key = Environment.GetEnvironmentVariable("logAnalyticsKey", EnvironmentVariableTarget.Process);
            services.AddSingleton<ILogAnalyticsClient>(_ => new LogAnalyticsClient(workspace, key));

            var vstsPat = Environment.GetEnvironmentVariable("vstsPat", EnvironmentVariableTarget.Process);

            services.AddSingleton<IVstsRestClient>(_ => new VstsRestClient("somecompany", vstsPat));
            services.AddSingleton<HttpClient>(_ => new HttpClient());

            services.AddScoped<IMemoryCache>(_ => new MemoryCache(new MemoryCacheOptions()));
            services.AddTransient<IProjectScan<SecurityReport>, SecurityReportScan>();
            services.AddTransient<IServiceHookScan<ReleaseDeploymentCompletedReport>, ReleaseDeploymentScan>();
            services.AddTransient<IServiceHookScan<BuildScanReport>, BuildScan>();
            services.AddTransient<IProjectScan<IEnumerable<RepositoryReport>>, RepositoryScan>();
            services.AddTransient<IServiceEndpointValidator, ServiceEndpointValidator>();
        }
    }
}