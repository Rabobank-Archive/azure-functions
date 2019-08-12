using System.Collections.Generic;
using System.Linq;
using Functions.Completeness.Requests;
using Functions.Completeness.Model;
using Microsoft.Azure.WebJobs;

namespace Functions.Completeness.Activities
{
    public class FilterProjectScannersActivity
    {
        [FunctionName(nameof(FilterProjectScannersActivity))]
        public IList<Orchestrator> Run([ActivityTrigger] SingleCompletenessCheckRequest request)
        {
            return request.AllProjectScanners
                .Where(i => GetParentId(i.InstanceId) == request.Supervisor.InstanceId)
                .ToList();
        }

        private static string GetParentId(string instanceId)
        {
            var idParts = instanceId.Split(':');
            return idParts.Length == 2 ? idParts.First() : string.Empty;
        }
    }
}