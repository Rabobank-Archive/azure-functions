using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using CompletenessCheckFunction.Requests;
using DurableFunctionsAdministration.Client.Response;
using System;

namespace CompletenessCheckFunction.Activities
{
    public class FilterAlreadyAnalyzedOrchestratorsActivity
    {
        [FunctionName(nameof(FilterAlreadyAnalyzedOrchestratorsActivity))]
        public IList<OrchestrationInstance> Run([ActivityTrigger] FilterAlreadyAnalyzedOrchestratorsActivityRequest request)
        {
            // Just return instances to analyze for now. Will implement filtering later
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return request.InstancesToAnalyze;
        }
    }
}


