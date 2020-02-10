using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using SecurePipelineScan.Rules.Events;
using SecurePipelineScan.Rules.Reports;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using System;
using System.Net.Http;
using LogAnalytics.Client;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Storage.Queue;
using Functions.Cmdb.Client;

[assembly: WebJobsStartup(typeof(Functions.Startup))]

namespace Functions
{
    public class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            RegisterServices(builder.Services);
        }

        private void RegisterServices(IServiceCollection services)
        {
            var tenantId = GetEnvironmentVariable("tenantId");
            var clientId = GetEnvironmentVariable("clientId");
            var clientSecret = GetEnvironmentVariable("clientSecret");

            var workspace = GetEnvironmentVariable("logAnalyticsWorkspace");
            var key = GetEnvironmentVariable("logAnalyticsKey");
            services.AddSingleton<ILogAnalyticsClient>(new LogAnalyticsClient(workspace, key,
                new AzureTokenProvider(tenantId, clientId, clientSecret)));

            var vstsPat = GetEnvironmentVariable("vstsPat");
            var organization = GetEnvironmentVariable("organization");
            var cmdbEndpoint = GetEnvironmentVariable("CmdbEndpoint");
            var cmdbApiKey = GetEnvironmentVariable("CmdbApiKey");
            var nonProdCiIdentifier = GetEnvironmentVariable("NonProdCiIdentifier");

            services.AddSingleton<IVstsRestClient>(new VstsRestClient(organization, vstsPat));
            services.AddSingleton<ICmdbClient>(new CmdbClient(new CmdbClientConfig(cmdbApiKey, cmdbEndpoint, organization, nonProdCiIdentifier)));

            services.AddScoped<IMemoryCache>(_ => new MemoryCache(new MemoryCacheOptions()));
            services.AddTransient<IServiceHookScan<ReleaseDeploymentCompletedReport>, ReleaseDeploymentScan>();
            services.AddTransient<IServiceHookScan<BuildScanReport>, BuildScan>();

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
            services.AddSingleton<IRulesProvider, RulesProvider>();
            services.AddSingleton<ITokenizer>(new Tokenizer(GetEnvironmentVariable("TOKEN_SECRET")));

            services.AddSingleton(new HttpClient());
        }

        private static string GetEnvironmentVariable(string variableName)
        {
            return Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.Process)
                   ?? throw new ArgumentNullException(variableName,
                       $"Please provide a valid value for environment variable '{variableName}'");
        }
    }
}