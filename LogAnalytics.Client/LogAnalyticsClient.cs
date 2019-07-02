using Flurl.Http;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LogAnalytics.Client
{
    /// <summary>
    /// Copied from: https://docs.microsoft.com/en-us/azure/azure-monitor/platform/data-collector-api#c-sample
    /// </summary>
    public class LogAnalyticsClient : ILogAnalyticsClient
    {
        private readonly string _workspace;
        private readonly string _key;

        public LogAnalyticsClient(string workspace, string key)
        {
            _workspace = workspace;
            _key = key;
        }

        public async Task AddCustomLogJsonAsync(string logName, object input, string timefield)
        {
            var json = JsonConvert.SerializeObject(input);

            // Create a hash for the API signature
            var datestring = DateTime.UtcNow.ToString("r");
            var jsonBytes = Encoding.UTF8.GetBytes(json);
            var stringToHash = "POST\n" + jsonBytes.Length + "\napplication/json\n" + "x-ms-date:" + datestring + "\n/api/logs";
            var hashedString = BuildSignature(stringToHash, _key);
            var signature = "SharedKey " + _workspace + ":" + hashedString;

            await PostData(logName, signature, datestring, json, timefield);
        }

        // Build the API signature
        private string BuildSignature(string message, string secret)
        {
            var encoding = new ASCIIEncoding();
            byte[] keyByte = Convert.FromBase64String(secret);
            byte[] messageBytes = encoding.GetBytes(message);
            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                byte[] hash = hmacsha256.ComputeHash(messageBytes);
                return Convert.ToBase64String(hash);
            }
        }

        // Send a request to the POST API endpoint
        private async Task PostData(string logname, string signature, string date, string json, string timefield)
        {
            var url = "https://" + _workspace + ".ods.opinsights.azure.com/api/logs?api-version=2016-04-01";

            var content = new StringContent(json, Encoding.UTF8);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            await url
                .WithHeader("Authorization", signature)
                .WithHeader("Log-Type", logname)
                .WithHeader("x-ms-date", date)
                .WithHeader("time-generated-field", timefield)
                .PostAsync(content);
        }
    }
}