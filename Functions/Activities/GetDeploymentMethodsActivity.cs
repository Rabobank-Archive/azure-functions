﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Functions.Cmdb.ProductionItems;
using Functions.Model;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Functions.Activities
{
    public class GetDeploymentMethodsActivity
    {
        private const string NONPROD = "NON-PROD";
        private readonly IProductionItemsRepository _productionItemsRepository;
        private readonly EnvironmentConfig _config;

        public GetDeploymentMethodsActivity(EnvironmentConfig config, IProductionItemsRepository productionItemRepository)
        {
            this._productionItemsRepository = productionItemRepository;
            this._config = config;
        }

        [FunctionName(nameof(GetDeploymentMethodsActivity))]
        public async Task<List<ProductionItem>> RunAsync([ActivityTrigger] string projectId)
        {
            if (projectId == null)
                throw new ArgumentNullException(nameof(projectId));

            var deploymentMethodEntities = await _productionItemsRepository.GetAsync(projectId).ConfigureAwait(false);

            return deploymentMethodEntities
                .GroupBy(d => d.PipelineId)
                .Select(g => new ProductionItem
                {
                    ItemId = g.Key,
                    DeploymentInfo = g.Select(x => x.CiIdentifier == _config.NonProdCiIdentifier ? ToNonProdCi(x) : x)
                                      .ToList()
                })
                .ToList();
        }

        private static DeploymentMethod ToNonProdCi(DeploymentMethod x) =>
            new DeploymentMethod(x.RowKey, x.PartitionKey)
            {
                CiIdentifier = NONPROD,
                CiName = x.CiName,
                IsSoxApplication = x.IsSoxApplication,
                Organization = x.Organization,
                PipelineId = x.PipelineId,
                ProjectId = x.ProjectId,
                StageId = NONPROD
            };
    }
}