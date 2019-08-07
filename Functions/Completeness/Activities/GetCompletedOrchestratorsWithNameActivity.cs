using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;

namespace Functions.Completeness.Activities
{
    public class GetCompletedOrchestratorsWithNameActivity
    {
        [FunctionName(nameof(GetCompletedOrchestratorsWithNameActivity))]
        public async Task<IList<DurableOrchestrationStatus>> Run([ActivityTrigger] string name,
            [OrchestrationClient] DurableOrchestrationClientBase client)
        {
            var orchestrators = (await client.GetStatusAsync())
                .Where(i => i.Name == name && i.RuntimeStatus == OrchestrationRuntimeStatus.Completed)
                .ToList();
            
            return orchestrators;
        }
    }
}