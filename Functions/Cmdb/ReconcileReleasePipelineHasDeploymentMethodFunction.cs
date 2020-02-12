using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using SecurePipelineScan.VstsService;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Requests = SecurePipelineScan.VstsService.Requests;
using Newtonsoft.Json;
using Functions.Cmdb.Client;
using SecurePipelineScan.VstsService.Security;

namespace Functions
{
    public class ReconcileReleasePipelineHasDeploymentMethodFunction
    {
        private readonly ITokenizer _tokenizer;
        private readonly ICmdbClient _cmdbClient;
        private readonly IReleasePipelineHasDeploymentMethodReconciler _reconciler;
        private readonly IVstsRestClient _vstsClient;
        private const int PermissionBit = 3;

        public ReconcileReleasePipelineHasDeploymentMethodFunction(IVstsRestClient vstsClient, ITokenizer tokenizer, ICmdbClient cmdbClient, IReleasePipelineHasDeploymentMethodReconciler reconciler)
        {
            _vstsClient = vstsClient;
            _tokenizer = tokenizer;
            _cmdbClient = cmdbClient;
            _reconciler = reconciler;
        }

        [FunctionName(nameof(ReconcileReleasePipelineHasDeploymentMethodFunction))]
        public async Task<IActionResult> ReconcileAsync([HttpTrigger(AuthorizationLevel.Anonymous, Route = "reconcile/{organization}/{project}/releasepipelines/ReleasePipelineHasDeploymentMethod/{item?}")]HttpRequestMessage request,
            string organization,
            string project,
            string item = null)
        {
            if (string.IsNullOrWhiteSpace(project))
                throw new ArgumentNullException(nameof(project));

            var id = _tokenizer.IdentifierFromClaim(request);

            if (id == null)
                return new UnauthorizedResult();

            var userId = GetUserIdFromQueryString(request);

            if (!(await HasPermissionToReconcileAsync(project, id, userId)))
                return new UnauthorizedResult();

            var (ciIdentifier, environment) = await GetData(request);

            return await _reconciler.ReconcileAsync(project, id, userId, ciIdentifier, environment);
        }

        private async Task<(string, string)> GetData(HttpRequestMessage request)
        {
            if (request.Content == null)
                return (null, null);

            var content = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
            dynamic data = JsonConvert.DeserializeObject(content);

            return ((string)data?.ciIdentifier, (string)data?.environment);
        }

        private static string GetUserIdFromQueryString(HttpRequestMessage request)
        {
            var query = request.RequestUri?.Query != null
                ? HttpUtility.ParseQueryString(request.RequestUri?.Query)
                : null;

            return query?.Get("userId");
        }

        private async Task<bool> HasPermissionToReconcileAsync(string project, string id, string userId = null)
        {
            var permissions = await _vstsClient.GetAsync(Requests.Permissions.PermissionsGroupProjectId(project, id)).ConfigureAwait(false);
            if (permissions == null)
            {
                permissions = await _vstsClient.GetAsync(Requests.Permissions.PermissionsGroupProjectId(project, userId)).ConfigureAwait(false);
                if (permissions == null)
                {
                    return false;
                }
            }

            return permissions.Security.Permissions.Any(x =>
                x.DisplayName == "Manage project properties" && x.PermissionId == PermissionBit);
        }
    }
}