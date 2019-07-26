using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;
using CompletenessCheckFunction.Requests;
using DurableFunctionsAdministration.Client.Response;

namespace CompletenessCheckFunction.Activities
{
    public class FilterAlreadyAnalyzedOrchestratorsActivity
    {
        [FunctionName(nameof(FilterAlreadyAnalyzedOrchestratorsActivity))]
        public async Task<List<OrchestrationInstance>> Run([ActivityTrigger] FilterAlreadyAnalyzedOrchestratorsActivityRequest request)
        {
            // Just return instances to analyze for now. Will implement filtering later
            return request.InstancesToAnalyze;
        }
    }
}


