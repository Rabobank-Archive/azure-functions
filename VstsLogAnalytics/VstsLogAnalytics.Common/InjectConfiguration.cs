using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SecurePipelineScan.VstsService;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;
using Rules.Reports;
using SecurePipelineScan.Rules;
using SecurePipelineScan.Rules.Events;
using SecurePipelineScan.Rules.Reports;
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
            services.AddScoped<ILogAnalyticsClient>(_ => new LogAnalyticsClient(workspace, key));

            var vstsPat = Environment.GetEnvironmentVariable("vstsPat", EnvironmentVariableTarget.Process);
            services.AddScoped<IVstsRestClient>(_ => new VstsRestClient("somecompany", vstsPat));
            services.AddScoped<IMemoryCache>(_ => new MemoryCache(new MemoryCacheOptions()));
            services.AddTransient<IProjectScan<SecurityReport>, SecurityReportScan>();
            services.AddTransient<IServiceHookScan<ReleaseDeploymentCompletedReport>, ReleaseDeploymentScan>();
            services.AddTransient<IProjectScan<IEnumerable<RepositoryReport>>, RepositoryScan>();
        }
    }
}