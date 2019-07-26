using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs;
using CompletenessCheckFunction.Requests;
using DurableFunctionsAdministration.Client.Response;

namespace CompletenessCheckFunction.Activities
{
    public class FilterOrchestratorsForParentIdActivity
    {
        [FunctionName(nameof(FilterOrchestratorsForParentIdActivity))]
        public List<OrchestrationInstance> Run([ActivityTrigger] FilterOrchestratorsForParentIdActivityRequest request)
        {
            return request.InstancesToFilter.Where(i => GetParentId(i.InstanceId) == request.ParentId).ToList();
        }

        private static string GetParentId(string instanceId)
        {
            var idParts = instanceId.Split(':');
            return idParts.Length > 1 ? idParts[idParts.Length - 2] : string.Empty;
        }
    }
}


