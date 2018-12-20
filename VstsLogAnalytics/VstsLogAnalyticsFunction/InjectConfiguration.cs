using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SecurePipelineScan.VstsService;
using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Caching.Memory;
using VstsLogAnalytics.Client;
using VstsLogAnalytics.Common;

[assembly: WebJobsStartup(typeof(InjectConfiguration), "VstsLogAnalyticsFunction")]

namespace VstsLogAnalytics.Common
{
    public class InjectConfiguration : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            RegisterLogAnalyticsClient(builder.Services);
            RegisterVstsRestClient(builder.Services);
            builder.Services.AddScoped<IMemoryCache>(_ => new MemoryCache(new MemoryCacheOptions()));
        }

        private static void RegisterVstsRestClient(IServiceCollection services)
        {
            var token = Environment.GetEnvironmentVariable("vstsPat", EnvironmentVariableTarget.Process);
            services.AddScoped<IVstsRestClient>(_ => new VstsRestClient("somecompany", token));
        }

        private static void RegisterLogAnalyticsClient(IServiceCollection services)
        {
            var workspace = Environment.GetEnvironmentVariable("logAnalyticsWorkspace", EnvironmentVariableTarget.Process);
            var key = Environment.GetEnvironmentVariable("logAnalyticsKey", EnvironmentVariableTarget.Process);
            services.AddScoped<ILogAnalyticsClient>(_ => new LogAnalyticsClient(workspace, key));
        }
    }
}