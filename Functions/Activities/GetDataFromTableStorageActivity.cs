//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Functions.Model;
//using Microsoft.Azure.WebJobs;
//using Microsoft.WindowsAzure.Storage.Table;
//using SecurePipelineScan.VstsService.Response;
//using Unmockable;

//namespace Functions.Activities
//{
//    public class GetDataFromTableStorageActivity
//    {
//        private readonly IUnmockable<CloudTableClient> _cloudTableClient;
//        private readonly EnvironmentConfig _config;

//        public GetDataFromTableStorageActivity(IUnmockable<CloudTableClient> cloudTableClient, EnvironmentConfig config)
//        {
//            _cloudTableClient = cloudTableClient;
//            _config = config;
//        }

//        [FunctionName(nameof(GetDataFromTableStorageActivity))]
//        public Task<ItemOrchestratorRequest> RunAsync([ActivityTrigger] Project project)
//        {
//            if (project == null)
//                throw new ArgumentNullException(nameof(project));

//            return RunInternalAsync(project);
//        }

//        private async Task<ItemOrchestratorRequest> RunInternalAsync(Project project)
//        {
//            var query = new TableQuery<DeploymentMethodEntity>().Where(TableQuery.CombineFilters(
//                TableQuery.GenerateFilterCondition("Organization", QueryComparisons.Equal, _config.Organization),
//                TableOperators.And,
//                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, project.Id)));
//            var table = _cloudTableClient.Execute(c => c.GetTableReference("DeploymentMethod"));

//            var deploymentMethodEntities = new List<DeploymentMethodEntity>();
//            TableContinuationToken continuationToken = null;
//            do
//            {
//                var page = await table.ExecuteQuerySegmentedAsync(query, continuationToken)
//                    .ConfigureAwait(false);
//                continuationToken = page.ContinuationToken;
//                deploymentMethodEntities.AddRange(page.Results);
//            }
//            while (continuationToken != null);

//            return new ItemOrchestratorRequest
//            {
//                Project = project,
//                ProductionItems = deploymentMethodEntities
//                    .GroupBy(d => d.PipelineId)
//                    .Select(g => new ProductionItem
//                    {
//                        ItemId = g.Key,
//                        CiIdentifiers = g.Select(x => x.CiIdentifier).ToList()
//                    })
//                    .ToList()
//            };
//        }
//    }
//}