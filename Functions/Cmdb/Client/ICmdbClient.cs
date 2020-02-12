using System.Threading.Tasks;
using Functions.Cmdb.Model;

namespace Functions.Cmdb.Client

{
    public interface ICmdbClient
    {
        CmdbClientConfig Config { get; }

        Task<CiContentItem> GetCiAsync(string ciIdentifier);

        Task<AssignmentContentItem> GetAssignmentAsync(string name);

        Task UpdateDeploymentMethodAsync(string item, CiContentItem update);
    }
}