using Functions.Model;
using SecurePipelineScan.VstsService.Response;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Functions.Helpers
{
    public static class LinkConfigurationItemHelper
    {
        public static IList<ProductionItem> LinkCisToBuildPipelines(
            IList<ReleaseDefinition> releasePipelines, IList<ProductionItem> productionItems,
            Project project)
        {
            if (releasePipelines == null)
                throw new ArgumentNullException(nameof(releasePipelines));
            if (productionItems == null)
                throw new ArgumentNullException(nameof(productionItems));
            if (project == null)
                throw new ArgumentNullException(nameof(project));

            var result = from r in releasePipelines
                         join p in productionItems on r.Id equals p.ItemId
                         from a in r.Artifacts
                         where a.Type == "Build" && a.DefinitionReference.Project?.Id == project.Id
                         select new ProductionItem
                         {
                             ItemId = a.DefinitionReference.Definition.Id,
                             CiIdentifiers = p.CiIdentifiers
                         };

            return GroupAndFilterProductionItems(result);
        }

        public static IList<ProductionItem> LinkCisToRepositories(
            IList<BuildDefinition> buildPipelines, IList<ProductionItem> productionItems,
            Project project)
        {
            if (buildPipelines == null)
                throw new ArgumentNullException(nameof(buildPipelines));
            if (productionItems == null)
                throw new ArgumentNullException(nameof(productionItems));
            if (project == null)
                throw new ArgumentNullException(nameof(project));

            var result = from b in buildPipelines
                         join p in productionItems on b.Id equals p.ItemId
                         where b.Repository?.Url != null && 
                            b.Repository.Url.ToString().Contains(project.Name)
                         select new ProductionItem
                         {
                             ItemId = b.Repository.Id,
                             CiIdentifiers = p.CiIdentifiers
                         };

            return GroupAndFilterProductionItems(result);
        }

        private static IList<ProductionItem> GroupAndFilterProductionItems(
            IEnumerable<ProductionItem> productionItems)
        {
            return productionItems
                .GroupBy(p => p.ItemId)
                .Select(g => new ProductionItem
                {
                    ItemId = g.Key,
                    CiIdentifiers = g
                        .SelectMany(p => p.CiIdentifiers)
                        .Distinct()
                        .ToList()
                })
                .ToList();
        }
    }
}