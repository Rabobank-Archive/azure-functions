using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;

namespace CompletenessCheckFunction.Orchestrators
{
    public class CompletenessCheckOrchestrator
    {
        [FunctionName(nameof(CompletenessCheckOrchestrator))]
        public async Task Run([OrchestrationTrigger] DurableOrchestrationContextBase context)
        {

        }
    }
}


