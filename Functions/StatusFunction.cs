using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Functions
{
    public class StatusFunction
    {
        private readonly EnvironmentConfig _config;

        public StatusFunction(EnvironmentConfig config)
        {
            _config = config;
        }

        [FunctionName("Status")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation($"The value of ExtensionName: {_config.ExtensionName}");
            return new OkObjectResult($"The value of ExtensionName: {_config.ExtensionName}");
        }
    }
}