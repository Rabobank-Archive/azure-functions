using System;
using System.Threading.Tasks;
using Functions.Model;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage.Table;
using SecurePipelineScan.VstsService.Response;
using Unmockable;

namespace Functions.Activities
{
    public class GetDataFromTableStorageActivity
    {
        private readonly IUnmockable<CloudTableClient> _cloudTableClient;
        private readonly EnvironmentConfig _config;

        public GetDataFromTableStorageActivity(IUnmockable<CloudTableClient> cloudTableClient, EnvironmentConfig config)
        {
            _cloudTableClient = cloudTableClient;
            _config = config;
        }

        [FunctionName(nameof(GetDataFromTableStorageActivity))]
        public Task<TableQuerySegment<DeploymentMethodEntity>> RunAsync([ActivityTrigger] Project project)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project));

            return RunInternalAsync(project);
        }

        private async Task<TableQuerySegment<DeploymentMethodEntity>> RunInternalAsync(Project project)
        {
            var query = new TableQuery<DeploymentMethodEntity>().Where(TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("Organisation", QueryComparisons.Equal, _config.Organization),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, project.Id)));
            var table = _cloudTableClient.Execute(c => c.GetTableReference("deploymentMethodTable"));
            return await table.ExecuteQuerySegmentedAsync(query, null).ConfigureAwait(false);
        }
    }
}