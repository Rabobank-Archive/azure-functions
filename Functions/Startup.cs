using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using SecurePipelineScan.VstsService;
using System;
using System.Net.Http;
using AzureDevOps.Compliance.Rules;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Storage.Queue;
using SecurePipelineScan.VstsService.Security;
using Functions.Helpers;
using Functions.Routing;

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

        private void RegisterServices(IServiceCollection services)
        {
            var vstsPat = GetEnvironmentVariable("vstsPat");
            var organization = GetEnvironmentVariable("organization");
            var nonProdCiIdentifier = GetEnvironmentVariable("NonProdCiIdentifier");

            services.AddSingleton<IVstsRestClient>(new VstsRestClient(organization, vstsPat));

            services.AddSingleton<IMemoryCache>(_ => new MemoryCache(new MemoryCacheOptions()));

            var extensionName = GetEnvironmentVariable("extensionName");
            var functionAppUrl = GetEnvironmentVariable("WEBSITE_HOSTNAME");
            //Microsoft.Azure.Storage.Queue
            // This only works because we use the account name and account key in the connection string.

            var connectionString = GetEnvironmentVariable("eventQueueStorageConnectionString");
            services.AddSingleton(CloudStorageAccount.Parse(connectionString).CreateCloudTableClient());
            var storage = Microsoft.Azure.Storage.CloudStorageAccount.Parse(connectionString);
            services.AddSingleton(storage.CreateCloudQueueClient());

            var config = new EnvironmentConfig
            {
                ExtensionName = extensionName,
                Organization = organization,
                FunctionAppHostname = functionAppUrl,
                EventQueueStorageAccountName = storage.Credentials.AccountName,
                EventQueueStorageAccountKey = Convert.ToBase64String(storage.Credentials.ExportKey()),
                NonProdCiIdentifier = nonProdCiIdentifier
            };

            services.AddSingleton(config);
            services.AddSingleton<ITokenizer>(new Tokenizer(GetEnvironmentVariable("TOKEN_SECRET")));

            services.AddDefaultRules();
            services.AddSingleton(new HttpClient());

            services.AddSingleton<IPoliciesResolver, PoliciesResolver>();
        }

        private static string GetEnvironmentVariable(string variableName)
        {
            return Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.Process)
                   ?? throw new ArgumentNullException(variableName,
                       $"Please provide a valid value for environment variable '{variableName}'");
        }
    }
}