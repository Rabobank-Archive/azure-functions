using System.Collections.Generic;
using System.Threading.Tasks;
using Functions.Model;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Functions.Activities
{
    public class GetConfigurationItemsFromTableStorageActivity
    {
        private readonly CloudTableClient _tableClient;

        public GetConfigurationItemsFromTableStorageActivity(CloudTableClient tableClient)
        {
            _tableClient = tableClient;
        }

        [FunctionName(nameof(GetConfigurationItemsFromTableStorageActivity))]
        public async Task<List<ConfigurationItem>> RunAsync([ActivityTrigger] DurableActivityContextBase context)
        {
            var table = _tableClient.GetTableReference("ConfigurationItem");
            var query = new TableQuery<ConfigurationItem>();

            var configItems = new List<ConfigurationItem>();
            TableContinuationToken continuationToken = null;

            do
            {
                var page = await table.ExecuteQuerySegmentedAsync(query, continuationToken)
                    .ConfigureAwait(false);
                continuationToken = page.ContinuationToken;
                configItems.AddRange(page.Results);
            } while (continuationToken != null);

            return configItems;
        }
    }
}