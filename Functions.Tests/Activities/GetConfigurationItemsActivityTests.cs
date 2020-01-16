using System;
using System.Threading.Tasks;
using AutoFixture;
using Functions.Activities;
using Functions.Model;
using Microsoft.Azure.Cosmos.Table;
using Xunit;

namespace Functions.Tests.Activities
{
    public class GetConfigurationItemsActivityTests
    {
        //When running on OSX you need a running azurite to make this test working
        [Fact]
        public async Task RunShouldReturnListOfConfigurationItems()
        {
            //Arrange
            var account = CloudStorageAccount.Parse("UseDevelopmentStorage=true"); 

            var client = account.CreateCloudTableClient();
            var table = client.GetTableReference("ConfigurationItem");
            
            await table.CreateIfNotExistsAsync().ConfigureAwait(false);

            var fixture = new Fixture();
            await table.ExecuteAsync(TableOperation.Insert(new ConfigurationItem
            {
                RowKey = Guid.NewGuid().ToString(),
                PartitionKey = fixture.Create<string>()
            })).ConfigureAwait(false);
            
            //Act
            var target = new GetConfigurationItemsFromTableStorageActivity(client);
            var configItems = await target.RunAsync(null);

            //Assert
            Assert.NotEmpty(configItems);
        }
    }
}