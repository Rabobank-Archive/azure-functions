using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;
using CompletenessCheckFunction.Requests;

namespace CompletenessCheckFunction.Orchestrators
{
    public class SingleAnalysisOrchestrator
    {
        [FunctionName(nameof(SingleAnalysisOrchestrator))]
        public async Task Run([OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            var singleAnalysisRequests = context.GetInput<SingleAnalysisOrchestratorRequest>();
        }
    }
}


