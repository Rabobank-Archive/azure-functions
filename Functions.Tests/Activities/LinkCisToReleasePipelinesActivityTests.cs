using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Threading.Tasks;
using Xunit;
using Shouldly;
using Functions.Model;
using Functions.Activities;
using Unmockable;
using AutoFixture;
using Response = SecurePipelineScan.VstsService.Response;

namespace Functions.Tests.Activities
{
    public class LinkCisToReleasePipelinesActivityTests
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
            var fun = new LinkCisToReleasePipelinesActivity(client.Wrap(),
                new EnvironmentConfig { Organization = organization });
            var result = await fun.RunAsync(project);

            //Assert
            result.Project.Id.ShouldBe(project.Id);
            result.ProductionItems.Count.ShouldBe(0);
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
            var fun = new LinkCisToReleasePipelinesActivity(client.Wrap(),
                new EnvironmentConfig { Organization = organization });
            var result = await fun.RunAsync(project);

            //Assert
            result.Project.Id.ShouldBe(project.Id);
            result.ProductionItems.Count.ShouldBe(0);
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
            var fun = new LinkCisToReleasePipelinesActivity(client.Wrap(),
                new EnvironmentConfig { Organization = organization });
            var result = await fun.RunAsync(project);

            //Assert
            result.Project.Id.ShouldBe(project.Id);
            result.ProductionItems.Count.ShouldBe(numberOfRows);
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
            var fun = new LinkCisToReleasePipelinesActivity(client.Wrap(),
                new EnvironmentConfig { Organization = organization });
            var result = await fun.RunAsync(project);

            //Assert
            result.Project.Id.ShouldBe(project.Id);
            result.ProductionItems.Count.ShouldBe(numberOfRows);
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
            var fun = new LinkCisToReleasePipelinesActivity(client.Wrap(),
                new EnvironmentConfig { Organization = organization });
            var result = await fun.RunAsync(project);

            //Assert
            result.Project.Id.ShouldBe(project.Id);
            result.ProductionItems.Count.ShouldBe(0);
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
            var fun = new LinkCisToReleasePipelinesActivity(client.Wrap(),
                new EnvironmentConfig { Organization = organization });
            var result = await fun.RunAsync(project);

            //Assert
            result.Project.Id.ShouldBe(project.Id);
            result.ProductionItems.Count.ShouldBe(0);
        }

        private static async Task CreateDummyTable(CloudTable table, string organization,
            string projectId, int count)
        {
            await table.DeleteIfExistsAsync().ConfigureAwait(false);
            await table.CreateIfNotExistsAsync().ConfigureAwait(false);

            var fixture = new Fixture();
            fixture.Customize<DeploymentMethodEntity>(ctx => ctx
                .With(x => x.Organization, organization)
                .With(x => x.ProjectId, projectId));

            foreach (var ci in fixture.CreateMany<DeploymentMethodEntity>(count))
            {
                await table.ExecuteAsync(TableOperation.Insert(ci))
                    .ConfigureAwait(false);
            }
        }
    }
}