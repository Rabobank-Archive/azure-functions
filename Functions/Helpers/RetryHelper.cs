using Polly;
using Polly.Retry;
using System;
using System.Diagnostics;
using Flurl.Http;

namespace Functions.Helpers
{
    public static class RetryHelper
    {
        public static readonly AsyncRetryPolicy InvalidDocumentVersionPolicy = Policy
            .Handle<FlurlHttpException>(ex => ex.Message.Contains("InvalidDocumentVersionException"))
            .RetryAsync(3);

        public static readonly AsyncRetryPolicy ServiceUnavailablePolicy = Policy
            .Handle<FlurlHttpException>(ex => ex.Message.Contains("Azure DevOps Services Unavailable"))
            .WaitAndRetryAsync(9, retryAttempt =>    // Max 9 retry attempts (= 2^9 / 60 = 8,5 minutes)
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, context) =>
                {
                    Trace.TraceWarning($"Caught exception {exception.Message}, " +
                        $"retrying after {timeSpan.TotalSeconds} seconds...");
                }
            );
    }
}
