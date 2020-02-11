using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Functions.Model;
using Microsoft.Azure.Cosmos.Table;

namespace Functions.ProductionItems
{

    public class DeploymentMethodsRepository : IDeploymentMethodsRepository
    {
        private readonly CloudTableClient _client;
        private readonly EnvironmentConfig _config;

        public DeploymentMethodsRepository(CloudTableClient client,
            EnvironmentConfig config)
        {
            _client = client;
            _config = config;
        }

        public async Task<List<DeploymentMethod>> GetAsync(string projectId)
        {
            if (projectId == null)
                throw new ArgumentNullException(nameof(projectId));

            var query = new TableQuery<DeploymentMethod>().Where(TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("Organization", QueryComparisons.Equal, _config.Organization),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("ProjectId", QueryComparisons.Equal, projectId)));
            var table = _client.GetTableReference("DeploymentMethod");

            if (!await table.ExistsAsync().ConfigureAwait(false))
                return new List<DeploymentMethod>();

            var deploymentMethodEntities = new List<DeploymentMethod>();
            TableContinuationToken continuationToken = null;
            do
            {
                var page = await table.ExecuteQuerySegmentedAsync(query, continuationToken)
                    .ConfigureAwait(false);
                continuationToken = page.ContinuationToken;
                deploymentMethodEntities.AddRange(page.Results);
            } while (continuationToken != null);

            return deploymentMethodEntities;
        }
    }
}