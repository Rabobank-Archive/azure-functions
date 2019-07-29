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
            RegisterServices(builder.Services);
        }

        private void RegisterServices(IServiceCollection services)
        {
            var tenantId = Environment.GetEnvironmentVariable("tenantId", EnvironmentVariableTarget.Process);
            var clientId = Environment.GetEnvironmentVariable("clientId", EnvironmentVariableTarget.Process);
            var clientSecret = Environment.GetEnvironmentVariable("clientSecret", EnvironmentVariableTarget.Process);
            var logAnalyticsWorkspace =
                Environment.GetEnvironmentVariable("logAnalyticsWorkspace", EnvironmentVariableTarget.Process);
            var logAnalyticsKey = Environment.GetEnvironmentVariable("logAnalyticsKey", EnvironmentVariableTarget.Process);
            services.AddSingleton<ILogAnalyticsClient>(new LogAnalyticsClient(logAnalyticsWorkspace, logAnalyticsKey,
                new AzureTokenProvider(tenantId, clientId, clientSecret)));

            var durableBaseUri =
                Environment.GetEnvironmentVariable("durableBaseUri", EnvironmentVariableTarget.Process);
            var durableTaskHub =
                Environment.GetEnvironmentVariable("durableTaskHub", EnvironmentVariableTarget.Process);
            var durableMasterKey =
                Environment.GetEnvironmentVariable("durableMasterKey", EnvironmentVariableTarget.Process);
            services.AddSingleton<IDurableFunctionsAdministrationClient>(
                new DurableFunctionsAdministrationClient(new Uri(durableBaseUri), durableTaskHub, durableMasterKey));
        }
    }
}