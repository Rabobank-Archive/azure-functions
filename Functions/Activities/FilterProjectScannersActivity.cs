using System.Collections.Generic;
using System.Linq;
using Functions.Completeness.Requests;
using Functions.Completeness.Model;
using Functions.Helpers;
using Microsoft.Azure.WebJobs;

namespace Functions.Completeness.Activities
{
    public class FilterProjectScannersActivity
    {
        [FunctionName(nameof(FilterProjectScannersActivity))]
        public IList<Orchestrator> Run([ActivityTrigger] SingleCompletenessCheckRequest request)
        {
            return request.AllProjectScanners
                .Where(i => OrchestrationIdHelper.GetSupervisorId(i.InstanceId) == request.Supervisor.InstanceId)
                .ToList();
        }
    }
}