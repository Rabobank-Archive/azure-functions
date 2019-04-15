using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace VstsLogAnalyticsFunction
{
    public class StatusFunction
    {
        private readonly IEnvironmentConfig config;

        public StatusFunction(IEnvironmentConfig config)
        {
            this.config = config;
        }

        [FunctionName("Status")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation($"The value of ExtensionName: {config.ExtensionName}");
            return new OkObjectResult($"The value of ExtensionName: {config.ExtensionName}");
        }
    }
}