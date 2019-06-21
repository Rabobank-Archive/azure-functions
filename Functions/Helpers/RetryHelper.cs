using Polly;
using Polly.Retry;
using System;
using System.Net;
using Flurl.Http;

namespace Functions.Helpers
{
    public static class RetryHelper
    {
        public static readonly AsyncRetryPolicy InvalidDocumentVersionPolicy = Policy
            .Handle<FlurlHttpException>(ex =>
                ex.Call.HttpStatus == HttpStatusCode.BadRequest && ex.Call.Request.IsExtMgtRequest())
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(5));
    }
}
