using System;
using System.Collections.Generic;
using System.Linq;
using Functions.Completeness.Requests;
using Microsoft.Azure.WebJobs;

namespace Functions.Completeness.Activities
{
    public class FilterAlreadyAnalyzedOrchestratorsActivity
    {
        [FunctionName(nameof(FilterAlreadyAnalyzedOrchestratorsActivity))]
        public IList<DurableOrchestrationStatus> Run([ActivityTrigger] FilterAlreadyAnalyzedOrchestratorsActivityRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return request.InstancesToAnalyze
                .Where(i => !request.InstanceIdsAlreadyAnalyzed.Contains(i.InstanceId))
                .ToList();
        }
    }
}


