using System;
using System.Net.Http;
using AzureDevOps.Compliance.Rules;
using Functions.Helpers;
using Functions.Routing;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Security;

[assembly: WebJobsStartup(typeof(Functions.Startup))]

namespace Functions
{
    public class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.AddRoutePriority();
            RegisterServices(builder.Services);
        }

        private static void RegisterServices(IServiceCollection services)
        {
            var token = GetEnvironmentVariable("TOKEN");
            var organization = GetEnvironmentVariable("ORGANIZATION");

            services.AddSingleton<IVstsRestClient>(new VstsRestClient(organization, token));
            services.AddSingleton<IMemoryCache>(_ => new MemoryCache(new MemoryCacheOptions()));

            var config = new EnvironmentConfig
            {
                ExtensionName = GetEnvironmentVariable("EXTENSION_NAME"),
                ExtensionPublisher = GetEnvironmentVariable("EXTENSION_PUBLISHER"),
                Organization = organization,
                FunctionAppHostname = GetEnvironmentVariable("WEBSITE_HOSTNAME"),
            };

            services.AddSingleton(config);
            services.AddSingleton<ITokenizer>(new Tokenizer(GetEnvironmentVariable("EXTENSION_SECRET")));

            services.AddDefaultRules();
            services.AddSingleton(new HttpClient());

            services.AddSingleton<IPoliciesResolver, PoliciesResolver>();
        }

        private static string GetEnvironmentVariable(string name)
        {
            return Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process)
                   ?? throw new ArgumentNullException(name,
                       $"Please provide a valid value for environment variable '{name}'");
        }
    }
}