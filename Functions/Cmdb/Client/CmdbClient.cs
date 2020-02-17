using Flurl;
using Flurl.Http;
using Functions.Cmdb.Model;
using System.Web;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Flurl.Http.Configuration;

namespace Functions.Cmdb.Client
{
    public class CmdbClient : ICmdbClient
    {
        public CmdbClientConfig Config { get; }

        public CmdbClient(CmdbClientConfig config)
        {
            this.Config = config ?? throw new System.ArgumentNullException(nameof(config));

            FlurlHttp.ConfigureClient(config.Endpoint, c =>
                {
                    c.Settings.JsonSerializer = new NewtonsoftJsonSerializer(
                        new JsonSerializerSettings { NullValueHandling = Newtonsoft.Json.NullValueHandling.Include }
                    );
                }
            );
        }

        public async Task<CiContentItem> GetCiAsync(string ciIdentifier) =>
                (await GetCiResponseAsync(ciIdentifier).ConfigureAwait(false))?
                    .Content?
                    .FirstOrDefault();

        public async Task<AssignmentContentItem> GetAssignmentAsync(string name) =>
            (await GetAssignmentsResponseAsync(name).ConfigureAwait(false))?
                .Content?
                .FirstOrDefault();

        public Task UpdateDeploymentMethodAsync(string item, CiContentItem update) => PostAsync($"{Config.Endpoint}devices/{HttpUtility.UrlEncode(item)}", update);

        private Task<GetCiResponse> GetCiResponseAsync(string ciIdentifier) => GetAsync<GetCiResponse>($"{Config.Endpoint}devices?CiIdentifier={ciIdentifier}");

        private Task<GetAssignmentsResponse> GetAssignmentsResponseAsync(string name) => GetAsync<GetAssignmentsResponse>($"{Config.Endpoint}assignments?name={HttpUtility.UrlEncode(name)}");

        private async Task<T> GetAsync<T>(string url) =>
            await url.SetQueryParam("view", "expand")
                     .WithHeader("somecompany-apikey", Config.ApiKey)
                     .WithHeader("content-type", "application/json")
                     .GetJsonAsync<T>()
                     .ConfigureAwait(false);

        private async Task PostAsync(string url, object data) =>
            await url.WithHeader("somecompany-apikey", Config.ApiKey)
                     .WithHeader("content-type", "application/json")
                     .PostJsonAsync(data)
                     .ConfigureAwait(false);
    }
}
