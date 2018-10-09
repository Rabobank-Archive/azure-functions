using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
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

            services.AddScoped<ILogAnalyticsClient>((_) => new LogAnalyticsClient(workspace, key));

            var vstsUrl = Environment.GetEnvironmentVariable("vstsUrl", EnvironmentVariableTarget.Process);
            var vstsPat = Environment.GetEnvironmentVariable("vstsPat", EnvironmentVariableTarget.Process);

            services.AddScoped<IVstsHttpClient>((_) => new VstsHttpClient(vstsUrl, vstsPat));
        }
    }
}