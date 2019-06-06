using Polly;
using Polly.Retry;
using SecurePipelineScan.VstsService;
using System;
using System.Diagnostics;

namespace Functions.Helpers
{
    public class RetryHelper
    {
        public static readonly RetryPolicy InvalidDocumentVersionPolicy = Policy
            .Handle<VstsException>(ex => ex.Message.Contains("InvalidDocumentVersionException"))
            .Retry(3);

        public static readonly RetryPolicy ServiceUnavailablePolicy = Policy
            .Handle<VstsException>(ex => ex.Message.Contains("Azure DevOps Services Unavailable"))
            .WaitAndRetry(9, retryAttempt =>    // Max 9 retry attempts (= 2^9 / 60 = 8,5 minutes)
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, context) =>
                {
                    Trace.TraceWarning($"Caught exception {exception.Message}, " +
                        $"retrying after {timeSpan.TotalSeconds} seconds...");
                }
            );
    }
}
