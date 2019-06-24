using Polly;
using Polly.Retry;
using System;
using System.Net;
using System.Threading.Tasks;
using Flurl.Http;
using SecurePipelineScan.VstsService;

namespace Functions.Helpers
{
    public static class RetryHelper
    {
        public static Task ExecuteInvalidDocumentVersionPolicy(string organization, Func<Task> action)
        {
            AsyncRetryPolicy invalidDocumentVersionPolicy = Policy
                .Handle<FlurlHttpException>(ex =>
                    ex.Call.HttpStatus == HttpStatusCode.BadRequest && ex.Call.Request.IsExtMgtRequest(organization))
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(5));

            return invalidDocumentVersionPolicy.ExecuteAsync(action);
        }
    }
}
