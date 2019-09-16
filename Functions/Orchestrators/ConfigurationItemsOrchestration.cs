using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;

namespace Functions.Orchestrators
{
    public class ConfigurationItemsOrchestration
    {
        [FunctionName(nameof(ConfigurationItemsOrchestration))]
        public async Task RunAsync([OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            
        }

    }
}