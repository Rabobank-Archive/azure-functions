using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SecurePipelineScan.Rules.Security;

namespace Functions.ProductionItems
{
    public class ProductionItemsResolver : IProductionItemsResolver
    {
        private readonly IDeploymentMethodsRepository _deploymentMethodsRepository;

        public ProductionItemsResolver(IDeploymentMethodsRepository deploymentMethodsRepository)
        {
            this._deploymentMethodsRepository = deploymentMethodsRepository;
        }

        public async Task<IEnumerable<string>> ResolveAsync(string project, string id)
        {
            var deploymentMethods = await _deploymentMethodsRepository.GetAsync(project).ConfigureAwait(false);

            return deploymentMethods.Where(x => x.PipelineId == id && !string.IsNullOrWhiteSpace(x.StageId))
                                    .Select(x => x.StageId)
                                    .Distinct()
                                    .ToList();
        }
    }
}