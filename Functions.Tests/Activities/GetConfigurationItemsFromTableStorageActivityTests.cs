using System;
using System.Threading.Tasks;
using AutoFixture;
using Functions.Activities;
using Functions.Model;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Xunit;

namespace Functions.Tests.Activities
{
    public class GetConfigurationItemsFromTableStorageActivityTests
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
            
            await table.ExecuteAsync(TableOperation.Insert(new ConfigurationItem
            {
                RowKey = Guid.NewGuid().ToString(),
                PartitionKey = new Fixture().Create<string>()
            })).ConfigureAwait(false);
            
            //Act
            var target = new GetConfigurationItemsFromTableStorageActivity(client);
            var configItems = await target.Run(null);

            //Assert
            Assert.NotEmpty(configItems);
        }
    }
}