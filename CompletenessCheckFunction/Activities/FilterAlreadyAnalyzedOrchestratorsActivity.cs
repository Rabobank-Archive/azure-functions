using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using CompletenessCheckFunction.Requests;
using DurableFunctionsAdministration.Client.Response;
using System;
using System.Linq;

namespace CompletenessCheckFunction.Activities
{
    public class FilterAlreadyAnalyzedOrchestratorsActivity
    {
        [FunctionName(nameof(FilterAlreadyAnalyzedOrchestratorsActivity))]
        public IList<OrchestrationInstance> Run([ActivityTrigger] FilterAlreadyAnalyzedOrchestratorsActivityRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return request.InstancesToAnalyze
                .Where(i => !request.InstanceIdsAlreadyAnalyzed.Contains(i.InstanceId))
                .ToList();
        }
    }
}


