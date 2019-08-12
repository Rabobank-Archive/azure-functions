using System;
using System.Collections.Generic;
using System.Linq;
using Functions.Completeness.Requests;
using Functions.Completeness.Model;
using Microsoft.Azure.WebJobs;

namespace Functions.Completeness.Activities
{
    public class FilterAlreadyAnalyzedOrchestratorsActivity
    {
        [FunctionName(nameof(FilterAlreadyAnalyzedOrchestratorsActivity))]
        public IList<SimpleDurableOrchestrationStatus> Run([ActivityTrigger] FilterAlreadyAnalyzedOrchestratorsActivityRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return request.InstancesToAnalyze
                .Where(i => !request.InstanceIdsAlreadyAnalyzed.Contains(i.InstanceId))
                .ToList();
        }
    }
}


