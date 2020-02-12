using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Functions
{
    public interface IReleasePipelineHasDeploymentMethodReconciler
    {
        Task<IActionResult> ReconcileAsync(string projectId, string itemId, string userId, string ciIdentifier, string environment);
    }
}