using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Threading.Tasks;
using Xunit;
using Shouldly;
using Functions.Model;

namespace Functions.Tests
{
    public class GetDataFromTableStorageActivityTests
    {
        [Fact]
        public async System.Threading.Tasks.Task GetDataFromLocalTableStorageAsync()
        {
            // Arrange 
            // Use Azure Storage Emulator on Windows or azurite to use local development server
            //   a) npm i -g azurite@2
            //   b) docker run --rm -p 10001:10001 arafato/azurite
            var storage = CloudStorageAccount.Parse("UseDevelopmentStorage=true");
            var client = storage.CreateCloudTableClient();
            var table = client.GetTableReference("ciDummyTable");
            await CreateDummyTable(table).ConfigureAwait(false);

            //Act
            var query = new TableQuery<DeploymentMethodEntity>().Where(TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("Organisation", QueryComparisons.Equal, "somecompany"), 
                TableOperators.And,
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "111")));
            var result = await table.ExecuteQuerySegmentedAsync(query, null);

            //Assert
            result.Results.Count.ShouldBe(2);
        }

        private async Task CreateDummyTable(CloudTable table)
        {
            await table.DeleteIfExistsAsync().ConfigureAwait(false);
            await table.CreateIfNotExistsAsync().ConfigureAwait(false);

            DeploymentMethodEntity ci1 = new DeploymentMethodEntity("1", "111")
            {
                CiIdentifier = "CI-1001",
                Organisation = "somecompany",
                PipelineId = "11",
                StageId = "1"
            };
            DeploymentMethodEntity ci2 = new DeploymentMethodEntity("2", "111")
            {
                CiIdentifier = "CI-1002",
                Organisation = "somecompany",
                PipelineId = "22",
                StageId = "2"
            };
            DeploymentMethodEntity ci3 = new DeploymentMethodEntity("3", "111")
            {
                CiIdentifier = "CI-1003",
                Organisation = "somecompany-dublin",
                PipelineId = "33",
                StageId = "3"
            };

            await table.ExecuteAsync(TableOperation.Insert(ci1));
            await table.ExecuteAsync(TableOperation.Insert(ci2));
            await table.ExecuteAsync(TableOperation.Insert(ci3));
        }
    }
}
