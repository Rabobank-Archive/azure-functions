using System;
using DurableFunctionsAdministration.Client;
using LogAnalytics.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;

[assembly: WebJobsStartup(typeof(CompletenessCheckFunction.Startup))]

namespace CompletenessCheckFunction
{
    public class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            RegisterServices(builder.Services);
        }

        private static void RegisterServices(IServiceCollection services)
        {
            var tenantId = GetEnvironmentVariable("tenantId");
            var clientId = GetEnvironmentVariable("clientId");
            var clientSecret = GetEnvironmentVariable("clientSecret");
            var logAnalyticsWorkspace = GetEnvironmentVariable("logAnalyticsWorkspace");
                
            var logAnalyticsKey = GetEnvironmentVariable("logAnalyticsKey");
            services.AddSingleton<ILogAnalyticsClient>(new LogAnalyticsClient(logAnalyticsWorkspace, logAnalyticsKey,
                new AzureTokenProvider(tenantId, clientId, clientSecret)));

            var durableBaseUri = GetEnvironmentVariable("durableBaseUri");
            var durableTaskHub = GetEnvironmentVariable("durableTaskHub");
            var durableMasterKey = GetEnvironmentVariable("durableMasterKey");
            services.AddSingleton<IDurableFunctionsAdministrationClient>(
                new DurableFunctionsAdministrationClient(new Uri(durableBaseUri), durableTaskHub, durableMasterKey));
        }

        private static string GetEnvironmentVariable(string variableName)
        {
            return Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.Process)
                   ?? throw new ArgumentNullException(variableName, $"Please provide a valid value for environment variable '{variableName}'");
        }
    }
}