using System.Collections.Generic;
using System.Threading.Tasks;
using Functions.Model;

namespace Functions.Cmdb.ProductionItems
{
    public interface IProductionItemsRepository
    {
        Task<List<DeploymentMethod>> GetAsync(string projectId);
    }
}