using System.Collections.Generic;
using System.Threading.Tasks;
using Functions.Model;

namespace Functions.ProductionItems
{
    public interface IDeploymentMethodsRepository
    {
        Task<List<DeploymentMethod>> GetAsync(string projectId);
    }
}