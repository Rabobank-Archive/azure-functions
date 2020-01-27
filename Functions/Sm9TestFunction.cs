using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Net.Http;
using System.Threading.Tasks;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using System;
using Flurl;
using Newtonsoft.Json.Linq;

namespace Functions
{
    public class Sm9TestFunction
    {
        [FunctionName(nameof(Sm9TestFunction))]
        public async Task<IActionResult> Test([HttpTrigger(AuthorizationLevel.Anonymous)]HttpRequestMessage request, ILogger logger)
        {
            try
            {
                var response = (await GetCiResponseAsync(Environment.GetEnvironmentVariable("CiIdentifier")).ConfigureAwait(false));
                logger.LogInformation(response.ToString());
            }
            catch (FlurlHttpException e)
            {
                logger.LogError(e.Message, e);
            }

            return new OkResult();
        }

        private Task<JObject> GetCiResponseAsync(string ciIdentifier) => GetAsync<JObject>($"{Environment.GetEnvironmentVariable("CmdbEndpoint")}devices?CiIdentifier={ciIdentifier}");

        private async Task<T> GetAsync<T>(string url) =>
            await url.SetQueryParam("view", "expand")
             .WithHeader("somecompany-apikey", Environment.GetEnvironmentVariable("CmdbApiKey"))
             .WithHeader("content-type", "application/json")
             .GetJsonAsync<T>()
             .ConfigureAwait(false);
    }
}