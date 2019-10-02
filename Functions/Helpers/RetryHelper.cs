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
        private const int FirstRetryInterval = 1 * 60; // First retry happens after 1 minute
        private const int MaxNumberOfAttempts = 10; // Maximum of 6 attempts
        private const double BackoffCoefficient = 1.5; // Back-off timer is multiplied by this number for each retry
        private const int MaxRetryInterval = 25 * 60; // Maximum time to wait
        private const int RetryTimeout = 5 * 60; // Time to wait before a single retry times out

        public static Task ExecuteInvalidDocumentVersionPolicyAsync(string organization, Func<Task> action)
        {
            AsyncRetryPolicy invalidDocumentVersionPolicy = Policy
                .Handle<FlurlHttpException>(ex =>
                    ex.Call.HttpStatus == HttpStatusCode.BadRequest && ex.Call.Request.IsExtMgtRequest(organization))
                .WaitAndRetryAsync(MaxNumberOfAttempts, retryAttempt => TimeSpan.FromSeconds(new Random().Next(5, 20)));

            return invalidDocumentVersionPolicy.ExecuteAsync(action);
        }

        public static RetryOptions ActivityRetryOptions => new RetryOptions(
            firstRetryInterval: TimeSpan.FromSeconds(FirstRetryInterval), 
            maxNumberOfAttempts: MaxNumberOfAttempts) 
            {
                BackoffCoefficient = BackoffCoefficient, 
                Handle = IsRetryableActivity,
                MaxRetryInterval = TimeSpan.FromSeconds(MaxRetryInterval), 
                RetryTimeout = TimeSpan.FromSeconds(RetryTimeout) 
            };

        private static bool IsRetryableActivity(Exception exception)
        {
            return exception.InnerException != null &&
                // Handle rate limits (happens if we got blocked by rate limits)
                (exception.InnerException.Message.Contains("Call failed with status code 429") ||
                // Handle timeout (happens if we got delayed by rate limits)
                exception.InnerException.Message.Contains(
                    "A connection attempt failed because the connected party did not properly respond after a period of time") ||
                exception.InnerException.Message.Contains(
                    "An existing connection was forcibly closed by the remote host"));
        }
    }
}