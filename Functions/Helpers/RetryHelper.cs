using Flurl.Http;
using Microsoft.Azure.WebJobs;
using Polly;
using Polly.Retry;
using SecurePipelineScan.VstsService;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

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

        public static RetryOptions ActivityRetryOptions => new RetryOptions(
                firstRetryInterval: TimeSpan.FromSeconds(1 * 60), // First retry happens after 1 minute
                maxNumberOfAttempts: 6) // Maximum of 6 attempts
            {
                BackoffCoefficient = 1.5, // Back-off timer is multiplied by this number for each retry
                Handle = IsRetryableActivity,
                MaxRetryInterval = TimeSpan.FromSeconds(25 * 60), // Maximum time to wait
                RetryTimeout = TimeSpan.FromSeconds(5 * 60) // Time to wait before a single retry times out
            };

        private static bool IsRetryableActivity(Exception exception)
        {
            return
                (exception.InnerException is FlurlHttpException
                     flurlHttpException && // Handle rate limits (happens if we got blocked by rate limits)
                 (flurlHttpException.Message.Contains("Call failed with status code 429") ||
                 flurlHttpException.Call.HttpStatus == (HttpStatusCode) 429))
                || (
                    exception.InnerException is SocketException
                        socketException && // Handle timeout (happens if we got delayed by rate limits)
                    socketException.Message.Contains(
                        "A connection attempt failed because the connected party did not properly respond after a period of time")
                )
                || (exception.InnerException is TaskCanceledException); // Happens when calls time out
        }
    }
}