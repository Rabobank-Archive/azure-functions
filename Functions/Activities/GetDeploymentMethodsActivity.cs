using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Functions.Model;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage.Table;
using SecurePipelineScan.VstsService.Response;

namespace Functions.Activities
{
    public class GetDeploymentMethodsActivity
    {
        private readonly CloudTableClient _client;
        private readonly EnvironmentConfig _config;

        public GetDeploymentMethodsActivity(CloudTableClient client, 
            EnvironmentConfig config)
        {
            _client = client;
            _config = config;
        }

        [FunctionName(nameof(GetDeploymentMethodsActivity))]
        public async Task<List<ProductionItem>> RunAsync([ActivityTrigger] Project project)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project));

            var query = new TableQuery<DeploymentMethod>().Where(TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("Organization", QueryComparisons.Equal, _config.Organization),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("ProjectId", QueryComparisons.Equal, project.Id)));
            var table = _client.GetTableReference("DeploymentMethod");

            if (!await table.ExistsAsync().ConfigureAwait(false))
                return new List<ProductionItem>();

            var deploymentMethodEntities = new List<DeploymentMethod>();
            TableContinuationToken continuationToken = null;
            do
            {
                var page = await table.ExecuteQuerySegmentedAsync(query, continuationToken)
                    .ConfigureAwait(false);
                continuationToken = page.ContinuationToken;
                deploymentMethodEntities.AddRange(page.Results);
            } while (continuationToken != null);

            return deploymentMethodEntities
                .GroupBy(d => d.PipelineId)
                .Select(g => new ProductionItem
                {
                    ItemId = g.Key,
                    DeploymentInfo = g.ToList()
                }).ToList();
        }
    }
}