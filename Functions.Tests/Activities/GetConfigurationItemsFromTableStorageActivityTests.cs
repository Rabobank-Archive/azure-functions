using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
            var account = CloudStorageAccount.DevelopmentStorageAccount;

            var client = account.CreateCloudTableClient();
            var table = client.GetTableReference("ConfigurationItem");
            
            await table.CreateIfNotExistsAsync();
            
            await table.ExecuteAsync(TableOperation.Insert(new ConfigurationItem
            {
                RowKey = Guid.NewGuid().ToString(),
            }));
            
            //Act
            var target = new GetConfigurationItemsFromTableStorageActivity(client);
            var configItems = await target.Run(null);

            //Assert
            Assert.NotEmpty(configItems);
        }
    }
}