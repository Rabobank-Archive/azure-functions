using System.Threading.Tasks;
using Xunit;
using Shouldly;
using Functions.Model;
using AutoFixture;
using Microsoft.Azure.Cosmos.Table;
using Response = SecurePipelineScan.VstsService.Response;
using Functions.ProductionItems;

namespace Functions.Tests.Activities
{
    public class DeploymentMethodsRepositoryTests
    {
        [Fact]
        public async Task ShouldReturnEmptyListIfTableDoesNotExist()
        {
            // Arrange 
            // Use Azure Storage Emulator on Windows or azurite to use local development server
            //   a) npm i -g azurite@2
            //   b) docker run --rm -p 10001:10001 arafato/azurite
            var storage = CloudStorageAccount.Parse("UseDevelopmentStorage=true");
            var client = storage.CreateCloudTableClient();
            var table = client.GetTableReference("DeploymentMethod");
            await table.DeleteIfExistsAsync().ConfigureAwait(false);

            var fixture = new Fixture();
            var project = fixture.Create<Response.Project>();
            var organization = fixture.Create<string>();

            // Act
            var fun = new DeploymentMethodsRepository(client,
                new EnvironmentConfig { Organization = organization });
            var result = await fun.GetAsync(project.Id);

            //Assert
            result.Count.ShouldBe(0);
        }

        [Fact]
        public async Task ShouldReturnEmptyListIfTableColumnDoesNotExist()
        {
            // Arrange 
            var storage = CloudStorageAccount.Parse("UseDevelopmentStorage=true");
            var client = storage.CreateCloudTableClient();
            var table = client.GetTableReference("DeploymentMethod");
            await table.DeleteIfExistsAsync().ConfigureAwait(false);
            await table.CreateIfNotExistsAsync().ConfigureAwait(false);

            var fixture = new Fixture();
            var project = fixture.Create<Response.Project>();
            var organization = fixture.Create<string>();

            // Act
            var fun = new DeploymentMethodsRepository(client,
                new EnvironmentConfig { Organization = organization });
            var result = await fun.GetAsync(project.Id);

            //Assert
            result.Count.ShouldBe(0);
        }

        [Fact]
        public async Task ShouldReturnProductionItemsForProjectWithinOrganization()
        {
            // Arrange 
            var storage = CloudStorageAccount.Parse("UseDevelopmentStorage=true");
            var client = storage.CreateCloudTableClient();
            var table = client.GetTableReference("DeploymentMethod");

            var fixture = new Fixture();
            var project = fixture.Create<Response.Project>();
            var organization = fixture.Create<string>();
            var numberOfRows = fixture.Create<int>();

            await CreateDummyTable(table, organization, project.Id, numberOfRows)
                .ConfigureAwait(false);

            // Act
            var fun = new DeploymentMethodsRepository(client,
                new EnvironmentConfig { Organization = organization });
            var result = await fun.GetAsync(project.Id);

            //Assert
            result.Count.ShouldBe(numberOfRows);
        }

        [Fact]
        public async Task ShouldReturnProductionItemsForBigTable()
        {
            // Arrange 
            var storage = CloudStorageAccount.Parse("UseDevelopmentStorage=true");
            var client = storage.CreateCloudTableClient();
            var table = client.GetTableReference("DeploymentMethod");

            var fixture = new Fixture();
            var project = fixture.Create<Response.Project>();
            var organization = fixture.Create<string>();
            var numberOfRows = 1500;

            await CreateDummyTable(table, organization, project.Id, numberOfRows)
                .ConfigureAwait(false);

            // Act
            var fun = new DeploymentMethodsRepository(client,
                new EnvironmentConfig { Organization = organization });
            var result = await fun.GetAsync(project.Id);

            //Assert
            result.Count.ShouldBe(numberOfRows);
        }

        [Fact]
        public async Task ShouldNotReturnProductionItemsForOtherProject()
        {
            // Arrange 
            var storage = CloudStorageAccount.Parse("UseDevelopmentStorage=true");
            var client = storage.CreateCloudTableClient();
            var table = client.GetTableReference("DeploymentMethod");

            var fixture = new Fixture();
            var project = fixture.Create<Response.Project>();
            var organization = fixture.Create<string>();
            var numberOfRows = fixture.Create<int>();

            await CreateDummyTable(table, organization, "otherProjectId", numberOfRows)
                .ConfigureAwait(false);

            // Act
            var fun = new DeploymentMethodsRepository(client,
                new EnvironmentConfig { Organization = organization });
            var result = await fun.GetAsync(project.Id);

            //Assert
            result.Count.ShouldBe(0);
        }

        [Fact]
        public async Task ShouldNotReturnProductionItemsForOtherOrganization()
        {
            // Arrange 
            var storage = CloudStorageAccount.Parse("UseDevelopmentStorage=true");
            var client = storage.CreateCloudTableClient();
            var table = client.GetTableReference("DeploymentMethod");

            var fixture = new Fixture();
            var project = fixture.Create<Response.Project>();
            var organization = fixture.Create<string>();
            var numberOfRows = fixture.Create<int>();

            await CreateDummyTable(table, "otherOrganization", project.Id, numberOfRows)
                .ConfigureAwait(false);

            // Act
            var fun = new DeploymentMethodsRepository(client,
                new EnvironmentConfig { Organization = organization });
            var result = await fun.GetAsync(project.Id);

            //Assert
            result.Count.ShouldBe(0);
        }

        private static async Task CreateDummyTable(CloudTable table, string organization,
            string projectId, int count)
        {
            await table.DeleteIfExistsAsync().ConfigureAwait(false);
            await table.CreateIfNotExistsAsync().ConfigureAwait(false);

            var fixture = new Fixture();
            fixture.Customize<DeploymentMethod>(ctx => ctx
                .With(x => x.Organization, organization)
                .With(x => x.ProjectId, projectId));

            foreach (var ci in fixture.CreateMany<DeploymentMethod>(count))
            {
                await table.ExecuteAsync(TableOperation.Insert(ci))
                    .ConfigureAwait(false);
            }
        }
    }
}