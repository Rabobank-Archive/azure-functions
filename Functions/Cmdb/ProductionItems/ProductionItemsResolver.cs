using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SecurePipelineScan.Rules.Security;

namespace Functions.Cmdb.ProductionItems
{
    public class ProductionItemsResolver : IProductionItemsResolver
    {
        private readonly IProductionItemsRepository _productionItemRepository;

        public ProductionItemsResolver(IProductionItemsRepository productionItemRepository)
        {
            _productionItemRepository = productionItemRepository;
        }

        public async Task<IEnumerable<string>> ResolveAsync(string project, string id)
        {
            var deploymentMethods = await _productionItemRepository.GetAsync(project).ConfigureAwait(false);

            return deploymentMethods.Where(x => x.PipelineId == id && !string.IsNullOrWhiteSpace(x.StageId))
                                    .Select(x => x.StageId)
                                    .Distinct()
                                    .ToList();
        }
    }
}