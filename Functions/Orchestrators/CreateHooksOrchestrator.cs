using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs;
using Response = SecurePipelineScan.VstsService.Response;
using System.Threading.Tasks;
using Functions.Activities;
using Functions.Helpers;
using System.Threading;

namespace Functions.Orchestrators
{
    public class CreateHooksOrchestrator
    {
        private const int TimerInterval = 2;

        [FunctionName(nameof(CreateHooksOrchestrator))]
        public async Task RunAsync([OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            var projects = await context.CallActivityWithRetryAsync<IList<Response.Project>>(
                nameof(GetProjectsActivity),
                RetryHelper.ActivityRetryOptions, 
                null);

            var hooks = await context.CallActivityWithRetryAsync<IList<Response.Hook>>(
                nameof(GetHooksActivity),
                RetryHelper.ActivityRetryOptions, 
                null);

            await context.CallActivityAsync(nameof(CreateStorageQueuesActivity), null);
            await Task.WhenAll(projects.Select(async(p, i) => await StartCreateHooksActivityWithTimerAsync(context, p, i, hooks)));
        }

        private async static Task StartCreateHooksActivityWithTimerAsync(
            DurableOrchestrationContextBase context, Response.Project project, int index, 
            IList<Response.Hook> hooks)
        {
            await context.CreateTimer(context.CurrentUtcDateTime.AddSeconds(index * TimerInterval), 
                CancellationToken.None);
            await context.CallActivityWithRetryAsync(nameof(CreateHooksActivity),
                RetryHelper.ActivityRetryOptions, (hooks, project));
        }
    }
}