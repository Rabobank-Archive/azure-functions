using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;
using CompletenessCheckFunction.Requests;
using DurableFunctionsAdministration.Client.Response;

namespace CompletenessCheckFunction.Activities
{
    public class FilterOrchestratorsForParentIdActivity
    {
        [FunctionName(nameof(FilterOrchestratorsForParentIdActivity))]
        public async Task<List<OrchestrationInstance>> Run([ActivityTrigger] FilterOrchestratorsForParentIdActivityRequest request)
        {
            return new List<OrchestrationInstance>();
        }
    }
}


