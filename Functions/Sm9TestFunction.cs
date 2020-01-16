using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Net.Http;
using System.Threading.Tasks;
using Flurl.Http;
using Microsoft.Extensions.Logging;

namespace Functions
{
    public class Sm9TestFunction
    {
        [FunctionName(nameof(Sm9TestFunction))]
        public async Task<IActionResult> Test([HttpTrigger(AuthorizationLevel.Anonymous)]HttpRequestMessage request, ILogger logger)
        {
            try
            {
                var response = await "https://sm9functiondev.ase-01.northeurope.azure.rabodev.com/api/ImportCmdbHttpStarter"
                        .PostJsonAsync(new
                        {
                            user = "chung.lok.lam@somecompany.nl",
                            ciIdentifier = "CI7679180"
                        });

                logger.LogInformation($"Status code is {response.StatusCode.ToString()}");
            }
            catch (FlurlHttpException e)
            {
                logger.LogError(e.Message, e);
            }

            return new OkResult();
        }
    }
}